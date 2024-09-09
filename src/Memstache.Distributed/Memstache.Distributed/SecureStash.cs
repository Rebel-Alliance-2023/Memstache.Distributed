using System;
using System.Threading.Tasks;
using MemStache.Distributed.KeyManagement;
using MemStache.Distributed.Encryption;
using MemStache.Distributed.Security;
using System.Text;

namespace MemStache.Distributed.Secure
{
    public class SecureStash<T> : IStash<T>
    {
        public string Key { get; set; }
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                StoredType = typeof(T).AssemblyQualifiedName;
            }
        }
        public string StoredType { get; private set; }
        public int Size { get; set; }
        public string Hash { get; set; }
        public DateTime ExpirationDate { get; set; }
        public StashPlan Plan { get; set; }
        
        // New properties for secure stash
        public string EncryptionKeyId { get; set; }
        public byte[] EncryptedData { get; set; }

        private readonly IKeyManagementService _keyManagementService;
        private readonly ICryptoService _cryptoService;

        public SecureStash(IKeyManagementService keyManagementService, ICryptoService cryptoService)
        {
            _keyManagementService = keyManagementService;
            _cryptoService = cryptoService;
            ExpirationDate = DateTime.MaxValue;
        }

        public async Task EncryptAsync()
        {
            if (Value == null) throw new InvalidOperationException("Cannot encrypt null value");

            var derivedKey = await _keyManagementService.GenerateDerivedKeyAsync();
            EncryptionKeyId = derivedKey.Id;

            var serializedData = System.Text.Json.JsonSerializer.Serialize(Value);
            var dataBytes = Encoding.UTF8.GetBytes(serializedData);
            EncryptedData = _cryptoService.EncryptData(derivedKey.PublicKey, dataBytes);

            // Clear the unencrypted value
            _value = default;
        }

        public async Task DecryptAsync()
        {
            if (EncryptedData == null) throw new InvalidOperationException("No encrypted data to decrypt");

            var derivedKey = await _keyManagementService.GetDerivedKeyAsync(EncryptionKeyId);
            if (derivedKey == null) throw new InvalidOperationException("Encryption key not found");

            var decryptedBytes = _cryptoService.DecryptData(derivedKey.PrivateKey, EncryptedData);
            var decryptedString = Encoding.UTF8.GetString(decryptedBytes);
            _value = System.Text.Json.JsonSerializer.Deserialize<T>(decryptedString);

            // Clear the encrypted data
            EncryptedData = null;
        }

        public async Task RotateKeyAsync()
        {
            await DecryptAsync();
            await EncryptAsync();
        }
    }

    public interface IStash<T>
    {
        string Key { get; set; }
        T Value { get; set; }
        string StoredType { get; }
        int Size { get; set; }
        string Hash { get; set; }
        DateTime ExpirationDate { get; set; }
        StashPlan Plan { get; set; }
    }
}
