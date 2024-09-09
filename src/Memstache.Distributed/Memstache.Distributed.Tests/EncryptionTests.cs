using System;
using System.Text;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Security;
using MemStache.Distributed.Encryption;

namespace MemStache.Distributed.Tests.Unit
{
    public class EncryptionTests
    {
        private readonly AesEncryptor _encryptor;

        public EncryptionTests()
        {
            var mockLogger = new Mock<ILogger<AesEncryptor>>();
            _encryptor = new AesEncryptor((Serilog.ILogger)mockLogger.Object);
        }

        [Fact]
        public void Encrypt_ShouldProduceDifferentOutput()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Sensitive data");
            var key = GenerateRandomKey();

            // Act
            var encrypted = _encryptor.Encrypt(data, key);

            // Assert
            Assert.NotEqual(data, encrypted);
        }

        [Fact]
        public void Decrypt_ShouldRestoreOriginalData()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Sensitive data");
            var key = GenerateRandomKey();
            var encrypted = _encryptor.Encrypt(data, key);

            // Act
            var decrypted = _encryptor.Decrypt(encrypted, key);

            // Assert
            Assert.Equal(data, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_LargeData_ShouldMaintainIntegrity()
        {
            // Arrange
            var largeData = Encoding.UTF8.GetBytes(new string('a', 1000000)); // 1MB of 'a'
            var key = GenerateRandomKey();

            // Act
            var encrypted = _encryptor.Encrypt(largeData, key);
            var decrypted = _encryptor.Decrypt(encrypted, key);

            // Assert
            Assert.Equal(largeData, decrypted);
        }

        [Fact]
        public void Decrypt_WithWrongKey_ShouldThrowException()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Sensitive data");
            var key1 = GenerateRandomKey();
            var key2 = GenerateRandomKey();
            var encrypted = _encryptor.Encrypt(data, key1);

            // Act & Assert
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => _encryptor.Decrypt(encrypted, key2));
        }

        private byte[] GenerateRandomKey()
        {
            var key = new byte[32]; // 256-bit key
            new Random().NextBytes(key);
            return key;
        }
    }
}
