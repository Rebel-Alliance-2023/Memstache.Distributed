using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Security;
using MemStache.Distributed.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace MemStache.Distributed.Tests.Unit
{
    public class MemStacheDistributedTests
    {
        private readonly Mock<IDistributedCacheProvider> _mockCacheProvider;
        private readonly Mock<ISerializer> _mockSerializer;
        private readonly Mock<ICompressor> _mockCompressor;
        private readonly Mock<ICryptoService> _mockCryptoService;
        private readonly Mock<IKeyManagementService> _mockKeyManagementService;
        private readonly Mock<ILogger<MemStacheDistributed>> _mockLogger;
        private readonly MemStacheOptions _options;
        private readonly MemStacheDistributed _memStache;
        private readonly IServiceProvider _serviceProvider; // Added for IServiceProvider

        public MemStacheDistributedTests()
        {
            _mockCacheProvider = new Mock<IDistributedCacheProvider>();
            _mockSerializer = new Mock<ISerializer>();
            _mockCompressor = new Mock<ICompressor>();
            _mockCryptoService = new Mock<ICryptoService>();
            _mockKeyManagementService = new Mock<IKeyManagementService>();
            _mockLogger = new Mock<ILogger<MemStacheDistributed>>();

            _options = new MemStacheOptions
            {
                EnableCompression = true,
                EnableEncryption = true
            };

            var mockOptionsSnapshot = new Mock<IOptions<MemStacheOptions>>();
            mockOptionsSnapshot.Setup(m => m.Value).Returns(_options);

            _serviceProvider = new ServiceCollection().BuildServiceProvider(); // Added for IServiceProvider

            _memStache = new MemStacheDistributed(
                (Factories.DistributedCacheProviderFactory)_mockCacheProvider.Object,
                (Factories.SerializerFactory)_mockSerializer.Object,
                (Factories.CompressorFactory)_mockCompressor.Object,
                _mockCryptoService.Object,
                _mockKeyManagementService.Object,
                mockOptionsSnapshot.Object,
                (Serilog.ILogger)_mockLogger.Object,
                _serviceProvider // Added for IServiceProvider
            );
        }

        [Fact]
        public async Task GetAsync_ShouldReturnDeserializedValue_WhenKeyExists()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedValue = new byte[] { 1, 2, 3 };
            var compressedValue = new byte[] { 4, 5, 6 };
            var encryptedValue = new byte[] { 7, 8, 9 };

            _mockCacheProvider.Setup(m => m.GetAsync(key, default)).ReturnsAsync(encryptedValue);

            _mockKeyManagementService.Setup(m => m.GetDerivedKeyAsync(key)).ReturnsAsync(new DerivedKey { PrivateKey = new byte[] { 10, 11, 12 } });

            var options = new MemStacheOptions
            {
                EnableCompression = true,
                EnableEncryption = true
            };
            _mockCryptoService.Setup(m => m.DecryptData(It.IsAny<byte[]>(), encryptedValue)).Returns(compressedValue);
            _mockCompressor.Setup(m => m.Decompress(compressedValue)).Returns(serializedValue);
            _mockSerializer.Setup(m => m.Deserialize<string>(serializedValue)).Returns(value);

            // Act
            var result = await _memStache.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task SetAsync_ShouldSerializeCompressEncryptAndStore()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            var serializedValue = new byte[] { 1, 2, 3 };
            var compressedValue = new byte[] { 4, 5, 6 };
            var encryptedValue = new byte[] { 7, 8, 9 };

            _mockSerializer.Setup(m => m.Serialize(value)).Returns(serializedValue);
            _mockCompressor.Setup(m => m.Compress(serializedValue)).Returns(compressedValue);
            _mockKeyManagementService.Setup(m => m.GenerateDerivedKeyAsync(null)).ReturnsAsync(new DerivedKey { PublicKey = new byte[] { 10, 11, 12 } });
            _mockKeyManagementService.Setup(m => m.GenerateDerivedKeyAsync(It.IsAny<string>())).ReturnsAsync(new DerivedKey { PublicKey = new byte[] { 10, 11, 12 } });

            // Remove the line causing the error
            // _serviceProvider = new ServiceCollection().BuildServiceProvider();

            _mockCryptoService.Setup(m => m.EncryptData(It.IsAny<byte[]>(), compressedValue)).Returns(encryptedValue);

            // Act
            await _memStache.SetAsync(key, value);

            // Assert
            _mockCacheProvider.Verify(m => m.SetAsync(key, encryptedValue, It.IsAny<MemStacheEntryOptions>(), default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldCallCacheProviderRemove()
        {
            // Arrange
            var key = "testKey";

            // Act
            await _memStache.RemoveAsync(key);

            // Assert
            _mockCacheProvider.Verify(m => m.RemoveAsync(key, default), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrueWhenKeyExists()
        {
            // Arrange
            var key = "testKey";
            _mockCacheProvider.Setup(m => m.ExistsAsync(key, default)).ReturnsAsync(true);

            // Act
            var result = await _memStache.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }
    }
}
