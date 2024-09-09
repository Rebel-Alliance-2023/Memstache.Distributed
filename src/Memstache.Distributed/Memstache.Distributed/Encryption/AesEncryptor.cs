using System;
using System.Security.Cryptography;
using Serilog;

namespace MemStache.Distributed.Encryption
{
    public class AesEncryptor : IEncryptor
    {
        private readonly ILogger _logger;

        public AesEncryptor(ILogger logger)
        {
            _logger = logger;
        }

        public byte[] Encrypt(byte[] data, byte[] key)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

                byte[] result = new byte[aes.IV.Length + encryptedData.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error encrypting data");
                throw;
            }
        }

        public byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;

                byte[] iv = new byte[aes.IV.Length];
                byte[] cipherText = new byte[encryptedData.Length - iv.Length];

                Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error decrypting data");
                throw;
            }
        }
    }
}
