using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MemStache.Distributed.KeyVaultManagement;

namespace MemStache.Distributed.Security
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly ICryptoService _cryptoService;
        private readonly IAzureKeyVaultSecretsWrapper _secretsClient;
        private readonly ConcurrentDictionary<string, MasterKey> _masterKeys = new();
        private readonly ConcurrentDictionary<string, DerivedKey> _derivedKeys = new();
        private const string default_masterKey = "default_masterKey";

        public KeyManagementService(ICryptoService cryptoService, IAzureKeyVaultSecretsWrapper secretsClient)
        {
            _cryptoService = cryptoService;
            _secretsClient = secretsClient;
        }

        public async Task<MasterKey> GenerateMasterKeyAsync()
        {
            var mnemonic = _cryptoService.GenerateMnemonic();
            var (publicKey, privateKey) = _cryptoService.GenerateKeyPairFromMnemonic(mnemonic);
            var masterKey = new MasterKey
            {
                Id = Guid.NewGuid().ToString(),
                PublicKey = publicKey,
                PrivateKey = privateKey
            };

            _masterKeys[default_masterKey] = masterKey;

            await _secretsClient.SetSecretAsync(default_masterKey, mnemonic);

            return masterKey;
        }

        public async Task<MasterKey> GetMasterKeyAsync(string keyId = null)
        {
            keyId = string.IsNullOrEmpty(keyId) ? default_masterKey : keyId;

            if (_masterKeys.TryGetValue(keyId, out var masterKey))
            {
                return masterKey;
            }

            // Retrieve from Azure KeyVault if not found locally
            var mnemonic = await _secretsClient.GetSecretAsync(keyId);
            if (mnemonic != null)
            {
                var keypair = _cryptoService.GenerateKeyPairFromMnemonic(mnemonic);

                masterKey = new MasterKey
                {
                    Id = keyId,
                    PublicKey = keypair.PublicKey,
                    PrivateKey = keypair.PrivateKey
                };

                // Optionally store the master key locally for future use
                _masterKeys[keyId] = masterKey;

                return masterKey;
            }

            return null;
        }

        public async Task<DerivedKey> GenerateDerivedKeyAsync(string keyId = null)
        {
            keyId = string.IsNullOrEmpty(keyId) ? default_masterKey : keyId;

            var masterKey = await GetMasterKeyAsync(keyId);
            if (masterKey == null)
            {
                throw new InvalidOperationException("Master key not found");
            }

            // Derive a new key from the master key
            var derivedPrivateKey = DerivePrivateKey(masterKey.PrivateKey);

            var derivedKey = new DerivedKey
            {
                Id = Guid.NewGuid().ToString(),
                MasterKeyId = masterKey.Id,
                PrivateKey = derivedPrivateKey,
                PublicKey = GetPublicKey(derivedPrivateKey)
            };

            _derivedKeys[derivedKey.Id] = derivedKey;

            return derivedKey;
        }

        public async Task<DerivedKey> GetDerivedKeyAsync(string keyId)
        {
            if (_derivedKeys.TryGetValue(keyId, out var derivedKey))
            {
                return derivedKey;
            }

            // Optionally, implement retrieval from storage if necessary
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
                // Re-derive the derived keys using the new master key
                var newDerivedPrivateKey = DerivePrivateKey(newMasterKey.PrivateKey);

                derivedKey.MasterKeyId = newMasterKey.Id;
                derivedKey.PrivateKey = newDerivedPrivateKey;
                derivedKey.PublicKey = GetPublicKey(newDerivedPrivateKey);
            }

            return newMasterKey;
        }

        // Helper methods for key derivation
        private byte[] DerivePrivateKey(byte[] masterPrivateKey)
        {
            // Example: Derive a new private key using HMAC-SHA256
            var hmac = new System.Security.Cryptography.HMACSHA256(masterPrivateKey);
            var derivedKeyBytes = hmac.ComputeHash(Guid.NewGuid().ToByteArray());
            return derivedKeyBytes;
        }

        private byte[] GetPublicKey(byte[] privateKeyBytes)
        {
            var privateKey = new NBitcoin.Key(privateKeyBytes);
            var publicKey = privateKey.PubKey;
            return publicKey.ToBytes();
        }
    }
}
