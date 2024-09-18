using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memstache.Distributed.KeyManagement;

namespace MemStache.Distributed.Security
{
    public class KeyManagementService : IKeyManagementService
    {
        
        private readonly ICryptoService _cryptoService;
        private readonly IAzureKeyVaultSecrets _secretsClient; // Use IAzureKeyVaultSecrets for Azure KeyVault operations
        private readonly ConcurrentDictionary<string, MasterKey> _masterKeys = new();
        private readonly ConcurrentDictionary<string, DerivedKey> _derivedKeys = new();

        public KeyManagementService(ICryptoService cryptoService, IAzureKeyVaultSecrets secretsClient)
        {
            _cryptoService = cryptoService;
            _secretsClient = secretsClient;
        }

        public async Task<MasterKey> GenerateMasterKeyAsync()
        {
            var (publicKey, privateKey) = _cryptoService.GenerateKeyPair();
            var masterKey = new MasterKey
            {
                Id = Guid.NewGuid().ToString(),
                PublicKey = publicKey,
                PrivateKey = privateKey
            };
            _masterKeys[masterKey.Id] = masterKey;

            // Store private key as a string in Azure KeyVault
            var privateKeyString = Convert.ToBase64String(privateKey);

            /*
              public async Task SetSecretAsync(string keyIdentifier, 
                                            string keyValue, 
                                            CancellationToken cancellationToken = default)
            */
            await _secretsClient.SetSecretAsync(masterKey.Id, privateKeyString);

            return masterKey;
        }

        public async Task<DerivedKey> GenerateDerivedKeyAsync(string masterKeyId = null)
        {
            if (string.IsNullOrEmpty(masterKeyId))
            {
                masterKeyId = _masterKeys.Keys.FirstOrDefault();
                if (masterKeyId == null)
                {
                    var newMasterKey = await GenerateMasterKeyAsync();
                    masterKeyId = newMasterKey.Id;
                }
            }

            if (!_masterKeys.TryGetValue(masterKeyId, out var masterKey))
            {
                // Retrieve master key from Azure KeyVault if not found locally
                var privateKeyString = await _secretsClient.GetSecretAsync(masterKeyId);
                byte[] privKey = Convert.FromBase64String(privateKeyString);
                masterKey = new MasterKey { Id = masterKeyId, PrivateKey = privKey };
                _masterKeys[masterKeyId] = masterKey;
            }

            var (publicKey, privateKey) = _cryptoService.GenerateKeyPair();
            var derivedKey = new DerivedKey
            {
                Id = Guid.NewGuid().ToString(),
                MasterKeyId = masterKeyId,
                PublicKey = publicKey,
                PrivateKey = privateKey
            };
            _derivedKeys[derivedKey.Id] = derivedKey;
            return derivedKey;
        }

        public async Task<MasterKey> GetMasterKeyAsync(string keyId)
        {
            if (_masterKeys.TryGetValue(keyId, out var masterKey))
            {
                return masterKey;
            }

            // Retrieve from Azure KeyVault if not found locally
            string privateKeyString = await _secretsClient.GetSecretAsync(keyId);
            if (privateKeyString != null)
            {
                var privateKey = Convert.FromBase64String(privateKeyString);
                masterKey = new MasterKey { Id = keyId, PrivateKey = privateKey };
                _masterKeys[keyId] = masterKey;
                return masterKey;
            }

            return null;
        }

        public async Task<DerivedKey> GetDerivedKeyAsync(string keyId)
        {
            if (_derivedKeys.TryGetValue(keyId, out var derivedKey))
            {
                return derivedKey;
            }
            return null;
        }

        public async Task<MasterKey> RotateMasterKeyAsync(string masterKeyId)
        {
            if (!_masterKeys.TryGetValue(masterKeyId, out var oldMasterKey))
            {
                throw new InvalidOperationException("Master key not found");
            }

            var newMasterKey = await GenerateMasterKeyAsync();
            _masterKeys.TryRemove(masterKeyId, out _);

            // Update all derived keys to use the new master key
            foreach (var derivedKey in _derivedKeys.Values.Where(dk => dk.MasterKeyId == masterKeyId))
            {
                derivedKey.MasterKeyId = newMasterKey.Id;
            }

            return newMasterKey;
        }
    }
}
