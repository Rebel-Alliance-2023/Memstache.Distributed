namespace MemStache.Distributed.Security
{
    using System.Threading.Tasks;

    public interface ICryptoService
    {
        string GenerateMnemonic();
        (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic);
        byte[] EncryptData(byte[] publicKey, byte[] data);
        byte[] DecryptData(byte[] privateKey, byte[] data);
        byte[] SignData(byte[] privateKey, byte[] data);
        bool VerifyData(byte[] publicKey, byte[] data, byte[] signature);

        Task<string> GenerateMnemonicAsync();
        Task<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairFromMnemonicAsync(string mnemonic);
        Task<byte[]> EncryptDataAsync(byte[] publicKey, byte[] data);
        Task<byte[]> DecryptDataAsync(byte[] privateKey, byte[] data);
        Task<byte[]> SignDataAsync(byte[] privateKey, byte[] data);
        Task<bool> VerifyDataAsync(byte[] publicKey, byte[] data, byte[] signature);
    }
}
