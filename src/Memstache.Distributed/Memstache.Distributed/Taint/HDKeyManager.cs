using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NBitcoin;
using MemStache.Distributed.Security;

namespace MemStache.Distributed.TaintStash
{
    public class HDKeyManager
    {
        private const int MaxLevel = 5;
        private readonly ConcurrentDictionary<string, ExtKey> _masterKeys = new();
        private readonly ConcurrentDictionary<string, ExtKey> _derivedKeys = new();
        private readonly ICryptoService _cryptoService;

        public HDKeyManager(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        }

        public async Task<string> CreateMasterKeyAsync()
        {
            var mnemonic = await _cryptoService.GenerateMnemonicAsync();
            var (publicKeyBytes, privateKeyBytes) = await _cryptoService.GenerateKeyPairFromMnemonicAsync(mnemonic);
            var privateKey = new Key(privateKeyBytes);
            var chainCode = new byte[32]; // You might want to generate this properly
            var masterKey = new ExtKey(privateKey, chainCode);
            var masterKeyId = Guid.NewGuid().ToString();
            _masterKeys[masterKeyId] = masterKey;

            return masterKeyId;
        }

        public async Task<string> DeriveLowerLevelKeyAsync(string parentKeyId, int targetLevel)
        {
            if (targetLevel <= 0 || targetLevel > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(targetLevel), $"Target level must be between 1 and {MaxLevel}");

            var parentKey = await GetKeyByIdAsync(parentKeyId);
            var parentLevel = GetKeyLevel(parentKey);

            if (targetLevel >= parentLevel)
                throw new ArgumentException($"Target level must be lower than the parent key level ({parentLevel})");

            var derivationPath = new KeyPath($"m/{MaxLevel - targetLevel}");
            var derivedKey = parentKey.Derive(derivationPath);
            var derivedKeyId = Guid.NewGuid().ToString();
            _derivedKeys[derivedKeyId] = derivedKey;

            return derivedKeyId;
        }

        public async Task<ExtKey> GetKeyByLevelAsync(int level)
        {
            if (level <= 0 || level > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level), $"Level must be between 1 and {MaxLevel}");

            var allKeys = _masterKeys.Values.Concat(_derivedKeys.Values);
            var key = allKeys.FirstOrDefault(k => GetKeyLevel(k) == level);

            if (key == null)
                throw new InvalidOperationException($"No key found for level {level}");

            return key;
        }

        public async Task<ExtKey> GetKeyByIdAsync(string keyId)
        {
            if (_masterKeys.TryGetValue(keyId, out var masterKey))
                return masterKey;

            if (_derivedKeys.TryGetValue(keyId, out var derivedKey))
                return derivedKey;

            throw new KeyNotFoundException($"Key with ID {keyId} not found");
        }

        private int GetKeyLevel(ExtKey key)
        {
            return MaxLevel - key.Derive(KeyPath.Parse("m")).Depth;
        }
    }
}
