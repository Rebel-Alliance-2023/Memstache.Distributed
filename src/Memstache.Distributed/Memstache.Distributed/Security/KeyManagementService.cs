using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace MemStache.Distributed.Security
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly ICryptoService _cryptoService;
        private readonly ConcurrentDictionary<string, MasterKey> _masterKeys = new();
        private readonly ConcurrentDictionary<string, DerivedKey> _derivedKeys = new();

        public KeyManagementService(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
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
                throw new InvalidOperationException("Master key not found");
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
