using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Security;
using MemStache.Distributed.KeyManagement;
using MemStache.Distributed.Factories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MemStache.Distributed.Tests.Unit
{
    public class MemStacheDistributedTests : IDisposable
    {
        private readonly Mock<IDistributedCacheProviderFactory> _mockCacheProviderFactory;
        private readonly Mock<ISerializerFactory> _mockSerializerFactory;
        private readonly Mock<ICompressorFactory> _mockCompressorFactory;
        private readonly Mock<IDistributedCacheProvider> _mockCacheProvider;
        private readonly Mock<ISerializer> _mockSerializer;
        private readonly Mock<ICompressor> _mockCompressor;
        private readonly Mock<ICryptoService> _mockCryptoService;
        private readonly Mock<IKeyManagementService> _mockKeyManagementService;
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly MemStacheOptions _options;
        private readonly MemStacheDistributed _memStache;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestOutputHelper _output;

        public MemStacheDistributedTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            _mockCacheProviderFactory = new Mock<IDistributedCacheProviderFactory>();
            _mockSerializerFactory = new Mock<ISerializerFactory>();
            _mockCompressorFactory = new Mock<ICompressorFactory>();
            _mockCacheProvider = new Mock<IDistributedCacheProvider>();
            _mockSerializer = new Mock<ISerializer>();
            _mockCompressor = new Mock<ICompressor>();
            _mockCryptoService = new Mock<ICryptoService>();
            _mockKeyManagementService = new Mock<IKeyManagementService>();

            _options = new MemStacheOptions
            {
                EnableCompression = true,
                EnableEncryption = true
            };

            var mockOptionsSnapshot = new Mock<IOptions<MemStacheOptions>>();
            mockOptionsSnapshot.Setup(m => m.Value).Returns(_options);

            _serviceProvider = new ServiceCollection().BuildServiceProvider();

            // Setup factory methods to return the mocked instances
            _mockCacheProviderFactory.Setup(f => f.Create(_serviceProvider)).Returns(_mockCacheProvider.Object);
            _mockSerializerFactory.Setup(f => f.Create(_serviceProvider)).Returns(_mockSerializer.Object);
            _mockCompressorFactory.Setup(f => f.Create(_serviceProvider)).Returns(_mockCompressor.Object);

            _memStache = new MemStacheDistributed(
                new DistributedCacheProviderFactory(sp => _mockCacheProvider.Object),
                new SerializerFactory(sp => _mockSerializer.Object),
                new CompressorFactory(sp => _mockCompressor.Object),
                _mockCryptoService.Object,
                _mockKeyManagementService.Object,
                mockOptionsSnapshot.Object,
                _serilogLogger, // Use the configured Serilog logger
                _serviceProvider
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
            _mockKeyManagementService.Setup(m => m.GenerateDerivedKeyAsync(It.IsAny<string>())).ReturnsAsync(new DerivedKey { PublicKey = new byte[] { 10, 11, 12 } });

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

        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
