using System;
using NBitcoin;
using NBitcoin.Crypto;

namespace MemStache.Distributed.Security
{
    public class CryptoService : ICryptoService
    {
        public string GenerateMnemonic()
        {
            var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            return mnemonic.ToString();
        }

        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic)
        {
            var mnemonicObj = new Mnemonic(mnemonic);
            var hdRoot = mnemonicObj.DeriveExtKey(); // HD root key derived from the mnemonic
            var privateKey = hdRoot.PrivateKey;
            var publicKey = privateKey.PubKey;

            // Export keys to byte arrays
            var privateKeyBytes = privateKey.ToBytes();
            var publicKeyBytes = publicKey.ToBytes();

            return (publicKeyBytes, privateKeyBytes);
        }

        public byte[] EncryptData(byte[] publicKeyBytes, byte[] data)
        {
            // Load public key
            var publicKey = new PubKey(publicKeyBytes);

            // Generate ephemeral key pair
            var ephemeralKey = new Key(); // Private key
            var ephemeralPubKey = ephemeralKey.PubKey; // Public key

            // Compute shared secret
            var sharedSecret = publicKey.GetSharedPubkey(ephemeralKey);

            // Derive symmetric key from shared secret
            var symmetricKey = Hashes.SHA256(sharedSecret.ToBytes());

            // Encrypt data using symmetric key (e.g., AES)
            var encryptedData = AesEncrypt(data, symmetricKey);

            // Combine ephemeral public key with encrypted data
            return Combine(ephemeralPubKey.ToBytes(), encryptedData);
        }

        public byte[] DecryptData(byte[] privateKeyBytes, byte[] encryptedMessage)
        {
            // Extract ephemeral public key and encrypted data
            var ephemeralPubKeyBytes = encryptedMessage[..33]; // First 33 bytes for compressed public key
            var encryptedData = encryptedMessage[33..];

            // Load keys
            var privateKey = new Key(privateKeyBytes);
            var ephemeralPubKey = new PubKey(ephemeralPubKeyBytes);

            // Compute shared secret
            var sharedSecret = ephemeralPubKey.GetSharedPubkey(privateKey);

            // Derive symmetric key from shared secret
            var symmetricKey = Hashes.SHA256(sharedSecret.ToBytes());

            // Decrypt data using symmetric key (e.g., AES)
            var decryptedData = AesDecrypt(encryptedData, symmetricKey);

            return decryptedData;
        }

        public byte[] SignData(byte[] privateKeyBytes, byte[] data)
        {
            var privateKey = new Key(privateKeyBytes);
            var hash = Hashes.SHA256(data); // Returns byte[]
            var signature = privateKey.Sign(new uint256(hash));
            return signature.ToDER();
        }

        public bool VerifyData(byte[] publicKeyBytes, byte[] data, byte[] signatureBytes)
        {
            var publicKey = new PubKey(publicKeyBytes);
            var hash = Hashes.SHA256(data); // Returns byte[]
            var signature = ECDSASignature.FromDER(signatureBytes);
            return publicKey.Verify(new uint256(hash), signature);
        }

        // Helper methods for AES encryption/decryption
        private byte[] AesEncrypt(byte[] data, byte[] key)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
            // Prepend IV for decryption
            return Combine(aes.IV, encryptedData);
        }

        private byte[] AesDecrypt(byte[] encryptedDataWithIv, byte[] key)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            var iv = encryptedDataWithIv[..16];
            var encryptedData = encryptedDataWithIv[16..];
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        private byte[] Combine(byte[] first, byte[] second)
        {
            var combined = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, combined, 0, first.Length);
            Buffer.BlockCopy(second, 0, combined, first.Length, second.Length);
            return combined;
        }
    }
}
