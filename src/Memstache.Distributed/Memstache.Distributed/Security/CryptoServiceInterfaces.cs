using System;
using System.Threading.Tasks;

namespace MemStache.Distributed.Security
{
    public interface IKeyManagementService
    {
        Task<MasterKey> GenerateMasterKeyAsync();
        Task<DerivedKey> GenerateDerivedKeyAsync(string masterKeyId = null);
        Task<MasterKey> GetMasterKeyAsync(string keyId);
        Task<DerivedKey> GetDerivedKeyAsync(string keyId);
        Task<MasterKey> RotateMasterKeyAsync(string masterKeyId);
    }

    public interface ICryptoService
    {
        (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair();
        byte[] EncryptData(byte[] publicKey, byte[] data);
        byte[] DecryptData(byte[] privateKey, byte[] data);
        byte[] SignData(byte[] privateKey, byte[] data);
        bool VerifyData(byte[] publicKey, byte[] data, byte[] signature);
        string GenerateMnemonic();
        (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic);
    }

    public class MasterKey
    {
        public string Id { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
    }

    public class DerivedKey
    {
        public string Id { get; set; }
        public string MasterKeyId { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
    }
}
