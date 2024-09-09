using System;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;

namespace MemStache.Distributed.Security
{
    public class CryptoService : ICryptoService
    {
        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportRSAPrivateKey();
            var publicKey = rsa.ExportRSAPublicKey();
            return (publicKey, privateKey);
        }

        public byte[] EncryptData(byte[] publicKey, byte[] data)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        public byte[] DecryptData(byte[] privateKey, byte[] data)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        public byte[] SignData(byte[] privateKey, byte[] data)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public bool VerifyData(byte[] publicKey, byte[] data, byte[] signature)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        // The rest of the methods remain unchanged
        public string GenerateMnemonic()
        {
            var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            return mnemonic.ToString();
        }

        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic)
        {
            var mnemonicObj = new Mnemonic(mnemonic);
            var seed = mnemonicObj.DeriveSeed();
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(seed[..256], out _);
            var privateKey = rsa.ExportRSAPrivateKey();
            var publicKey = rsa.ExportRSAPublicKey();
            return (publicKey, privateKey);
        }
    }
}
