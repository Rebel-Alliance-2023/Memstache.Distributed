using System;
using System.Text;
using Xunit;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Security;
using MemStache.Distributed.Encryption;

namespace MemStache.Distributed.Tests.Unit
{
    public class EncryptionTests : IDisposable
    {
        private readonly AesEncryptor _encryptor;
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly ITestOutputHelper _output;

        public EncryptionTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            // Use Serilog logger directly for AesEncryptor
            _encryptor = new AesEncryptor(_serilogLogger);
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

        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
