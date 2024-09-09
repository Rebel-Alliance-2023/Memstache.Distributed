using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MemStache.Distributed;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Encryption;
using MemStache.Distributed.KeyManagement;
using MemStache.Distributed.EvictionPolicies;
using MemStache.Distributed.MultiTenancy;
using MemStache.Distributed.Warmup;
using Microsoft.Extensions.Options;
using Serilog;

namespace MemStache.Distributed.Tests.Unit
{
    // Existing MemStacheDistributedTests and LruEvictionPolicyTests here...

    public class SystemTextJsonSerializerTests
    {
        private readonly SystemTextJsonSerializer _serializer;

        public SystemTextJsonSerializerTests()
        {
            _serializer = new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions(), Mock.Of<ILogger>());
        }

        [Fact]
        public void Serialize_ShouldReturnValidBytes()
        {
            // Arrange
            var testObject = new { Name = "Test", Value = 42 };

            // Act
            var result = _serializer.Serialize(testObject);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void Deserialize_ShouldReturnValidObject()
        {
            // Arrange
            var testObject = new { Name = "Test", Value = 42 };
            var serialized = _serializer.Serialize(testObject);

            // Act
            var result = _serializer.Deserialize<dynamic>(serialized);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", (string)result.Name);
            Assert.Equal(42, (int)result.Value);
        }
    }

    public class GzipCompressorTests
    {
        private readonly GzipCompressor _compressor;

        public GzipCompressorTests()
        {
            _compressor = new GzipCompressor(Mock.Of<ILogger>());
        }

        [Fact]
        public void Compress_ShouldReduceDataSize()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes(new string('a', 1000));

            // Act
            var compressed = _compressor.Compress(data);

            // Assert
            Assert.True(compressed.Length < data.Length);
        }

        [Fact]
        public void Decompress_ShouldRestoreOriginalData()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("Test data for compression");
            var compressed = _compressor.Compress(originalData);

            // Act
            var decompressed = _compressor.Decompress(compressed);

            // Assert
            Assert.Equal(originalData, decompressed);
        }
    }

    public class AesEncryptorTests
    {
        private readonly AesEncryptor _encryptor;

        public AesEncryptorTests()
        {
            _encryptor = new AesEncryptor(Mock.Of<ILogger>());
        }

        [Fact]
        public void Encrypt_ShouldProduceDifferentOutput()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Sensitive data");
            var key = new byte[32]; // 256-bit key
            new Random().NextBytes(key);

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
            var key = new byte[32]; // 256-bit key
            new Random().NextBytes(key);
            var encrypted = _encryptor.Encrypt(data, key);

            // Act
            var decrypted = _encryptor.Decrypt(encrypted, key);

            // Assert
            Assert.Equal(data, decrypted);
        }
    }

    public class RedisDistributedCacheProviderTests
    {
        private readonly Mock<IDatabase> _mockRedisDb;
        private readonly RedisDistributedCacheProvider _cacheProvider;

        public RedisDistributedCacheProviderTests()
        {
            _mockRedisDb = new Mock<IDatabase>();
            var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
            mockConnectionMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockRedisDb.Object);

            var mockOptions = new Mock<IOptions<RedisOptions>>();
            mockOptions.Setup(m => m.Value).Returns(new RedisOptions { Configuration = "localhost" });

            _cacheProvider = new RedisDistributedCacheProvider(mockOptions.Object, Mock.Of<ILogger>());
        }

        [Fact]
        public async Task GetAsync_ShouldReturnCachedValue()
        {
            // Arrange
            var key = "testKey";
            var expectedValue = Encoding.UTF8.GetBytes("testValue");
            _mockRedisDb.Setup(m => m.StringGetAsync(key, CommandFlags.None)).ReturnsAsync(expectedValue);

            // Act
            var result = await _cacheProvider.GetAsync(key);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public async Task SetAsync_ShouldSetValueInCache()
        {
            // Arrange
            var key = "testKey";
            var value = Encoding.UTF8.GetBytes("testValue");
            var options = new MemStacheEntryOptions { AbsoluteExpiration = TimeSpan.FromMinutes(5) };

            // Act
            await _cacheProvider.SetAsync(key, value, options);

            // Assert
            _mockRedisDb.Verify(m => m.StringSetAsync(key, value, It.IsAny<TimeSpan?>(), When.Always, CommandFlags.None), Times.Once);
        }
    }

    public class TenantManagerTests
    {
        private readonly Mock<IMemStacheDistributed> _mockBaseCache;
        private readonly TenantManager _tenantManager;

        public TenantManagerTests()
        {
            _mockBaseCache = new Mock<IMemStacheDistributed>();
            _tenantManager = new TenantManager(_mockBaseCache.Object, () => "tenant1", Mock.Of<ILogger>());
        }

        [Fact]
        public async Task GetAsync_ShouldPrefixKeyWithTenantId()
        {
            // Arrange
            var key = "testKey";
            var expectedValue = "testValue";
            _mockBaseCache.Setup(m => m.GetAsync<string>("tenant1:testKey", default)).ReturnsAsync(expectedValue);

            // Act
            var result = await _tenantManager.GetAsync<string>(key);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public async Task SetAsync_ShouldPrefixKeyWithTenantId()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";

            // Act
            await _tenantManager.SetAsync(key, value);

            // Assert
            _mockBaseCache.Verify(m => m.SetAsync("tenant1:testKey", value, null, default), Times.Once);
        }
    }

    public class CacheWarmerTests
    {
        private readonly Mock<IMemStacheDistributed> _mockCache;
        private readonly Mock<ICacheSeeder> _mockSeeder;
        private readonly CacheWarmer _cacheWarmer;

        public CacheWarmerTests()
        {
            _mockCache = new Mock<IMemStacheDistributed>();
            _mockSeeder = new Mock<ICacheSeeder>();
            _cacheWarmer = new CacheWarmer(_mockCache.Object, new[] { _mockSeeder.Object }, Mock.Of<ILogger>());
        }

        [Fact]
        public async Task StartAsync_ShouldCallSeedCacheAsyncOnAllSeeders()
        {
            // Act
            await _cacheWarmer.StartAsync(default);

            // Assert
            _mockSeeder.Verify(m => m.SeedCacheAsync(_mockCache.Object, default), Times.Once);
        }
    }
}
