using System;
using System.Threading.Tasks;

namespace MemStache.Distributed.Security
{

    public interface ICryptoService
    {
        string GenerateMnemonic();
        (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic);
        byte[] EncryptData(byte[] publicKey, byte[] data);
        byte[] DecryptData(byte[] privateKey, byte[] data);
        byte[] SignData(byte[] privateKey, byte[] data);
        bool VerifyData(byte[] publicKey, byte[] data, byte[] signature);
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
