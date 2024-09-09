using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MemStache.Distributed;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Encryption;
using MemStache.Distributed.KeyManagement;
using Microsoft.Extensions.Options;
using Serilog;
using MemStache.Distributed.EvictionPolicies;
using MemStache.Distributed.MultiTenancy;
using MemStache.Distributed.Warmup;
using StackExchange.Redis;
using System.Text;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Keys;
using Azure;
using MemStache.Distributed.Factories;
using MemStache.Distributed.Performance;

namespace MemStache.Distributed.Tests.Unit
{
    public class MemStacheDistributedTests
    {
        private readonly Mock<IDistributedCacheProvider> _mockCacheProvider;
        private readonly Mock<ISerializer> _mockSerializer;
        private readonly Mock<ICompressor> _mockCompressor;
        private readonly Mock<IEncryptor> _mockEncryptor;
        private readonly Mock<IKeyManager> _mockKeyManager;
        private readonly Mock<ILogger> _mockLogger;
        private readonly MemStacheOptions _options;
        private readonly MemStacheDistributed _memStache;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public MemStacheDistributedTests()
        {
            _mockCacheProvider = new Mock<IDistributedCacheProvider>();
            _mockSerializer = new Mock<ISerializer>();
            _mockCompressor = new Mock<ICompressor>();
            _mockEncryptor = new Mock<IEncryptor>();
            _mockKeyManager = new Mock<IKeyManager>();
            _mockLogger = new Mock<ILogger>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _options = new MemStacheOptions
            {
                EnableCompression = true,
                EnableEncryption = true
            };

            var mockOptionsSnapshot = new Mock<IOptions<MemStacheOptions>>();
            mockOptionsSnapshot.Setup(m => m.Value).Returns(_options);

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IDistributedCacheProvider))).Returns(_mockCacheProvider.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ISerializer))).Returns(_mockSerializer.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICompressor))).Returns(_mockCompressor.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEncryptor))).Returns(_mockEncryptor.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IKeyManager))).Returns(_mockKeyManager.Object);

            _memStache = new MemStacheDistributed(
                new DistributedCacheProviderFactory(sp => _mockCacheProvider.Object),
                new SerializerFactory(sp => _mockSerializer.Object),
                new CompressorFactory(sp => _mockCompressor.Object),
                new EncryptorFactory(sp => _mockEncryptor.Object),
                new KeyManagerFactory(sp => _mockKeyManager.Object),
                mockOptionsSnapshot.Object,
                _mockLogger.Object,
                _mockServiceProvider.Object
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
            _mockKeyManager.Setup(m => m.GetEncryptionKeyAsync(key, default)).ReturnsAsync(new byte[] { 10, 11, 12 });
            _mockEncryptor.Setup(m => m.Decrypt(encryptedValue, It.IsAny<byte[]>())).Returns(compressedValue);
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
            _mockKeyManager.Setup(m => m.GetEncryptionKeyAsync(key, default)).ReturnsAsync(new byte[] { 10, 11, 12 });
            _mockEncryptor.Setup(m => m.Encrypt(compressedValue, It.IsAny<byte[]>())).Returns(encryptedValue);

            // Act
            await _memStache.SetAsync(key, value);

            // Assert
            _mockCacheProvider.Verify(m => m.SetAsync(key, encryptedValue, It.IsAny<MemStacheEntryOptions>(), default), Times.Once);
        }
    }

    public class LruEvictionPolicyTests
    {
        [Fact]
        public void SelectVictim_ShouldReturnLeastRecentlyUsedKey()
        {
            // Arrange
            var policy = new LruEvictionPolicy(Mock.Of<ILogger>());
            policy.RecordAccess("key1");
            policy.RecordAccess("key2");
            policy.RecordAccess("key3");
            policy.RecordAccess("key1");

            // Act
            var victim = policy.SelectVictim();

            // Assert
            Assert.Equal("key2", victim);
        }
    }


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

    public partial class RedisDistributedCacheProviderTests
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


    public class SerializationAndCompressionTests
    {
        private readonly SystemTextJsonSerializer _serializer;
        private readonly GzipCompressor _compressor;

        public SerializationAndCompressionTests()
        {
            _serializer = new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions(), Mock.Of<ILogger>());
            _compressor = new GzipCompressor(Mock.Of<ILogger>());
        }

        [Fact]
        public void SerializeAndCompress_LargeObject_ShouldReduceSize()
        {
            // Arrange
            var largeObject = new
            {
                Id = Guid.NewGuid(),
                Name = "Large Object",
                Description = new string('A', 10000), // 10KB string
                Numbers = Enumerable.Range(1, 1000).ToList()
            };

            // Act
            var serialized = _serializer.Serialize(largeObject);
            var compressed = _compressor.Compress(serialized);

            // Assert
            Assert.True(compressed.Length < serialized.Length);
        }

        [Fact]
        public void SerializeAndDeserialize_ComplexObject_ShouldMaintainData()
        {
            // Arrange
            var complexObject = new
            {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Nested = new { Name = "Nested Object", Value = 42 },
                List = new List<string> { "Item1", "Item2", "Item3" }
            };

            // Act
            var serialized = _serializer.Serialize(complexObject);
            var deserialized = _serializer.Deserialize<dynamic>(serialized);

            // Assert
            Assert.Equal(complexObject.Id.ToString(), deserialized.Id.ToString());
            Assert.Equal(complexObject.Date.ToString(), DateTime.Parse(deserialized.Date.ToString()).ToString());
            Assert.Equal(complexObject.Nested.Name, (string)deserialized.Nested.Name);
            Assert.Equal(complexObject.Nested.Value, (int)deserialized.Nested.Value);
            Assert.Equal(complexObject.List.Count, deserialized.List.GetArrayLength());
        }
    }

    public class EvictionPolicyTests
    {
        [Fact]
        public void LfuEvictionPolicy_ShouldEvictLeastFrequentlyUsedItem()
        {
            // Arrange
            var policy = new LfuEvictionPolicy(Mock.Of<ILogger>());
            policy.RecordAccess("key1");
            policy.RecordAccess("key2");
            policy.RecordAccess("key3");
            policy.RecordAccess("key2");
            policy.RecordAccess("key3");
            policy.RecordAccess("key3");

            // Act
            var victim = policy.SelectVictim();

            // Assert
            Assert.Equal("key1", victim);
        }

        [Fact]
        public void TimeBasedEvictionPolicy_ShouldEvictExpiredItem()
        {
            // Arrange
            var policy = new TimeBasedEvictionPolicy(Mock.Of<ILogger>());
            policy.SetExpiration("key1", DateTime.UtcNow.AddSeconds(1));
            policy.SetExpiration("key2", DateTime.UtcNow.AddSeconds(2));
            policy.SetExpiration("key3", DateTime.UtcNow.AddSeconds(-1)); // Already expired

            // Act
            var victim = policy.SelectVictim();

            // Assert
            Assert.Equal("key3", victim);
        }
    }

    public class BatchOperationManagerTests
    {
        [Fact]
        public async Task BatchOperationManager_ShouldPreventDuplicateOperations()
        {
            // Arrange
            var manager = new BatchOperationManager<string, int>(Mock.Of<ILogger>());
            var operationCount = 0;

            Func<string, Task<int>> operation = async (key) =>
            {
                await Task.Delay(10); // Simulate some work
                return Interlocked.Increment(ref operationCount);
            };

            // Act
            var tasks = Enumerable.Range(0, 10).Select(_ => manager.GetOrAddAsync("testKey", operation));
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(1, operationCount);
            Assert.All(results, r => Assert.Equal(1, r));
        }
    }

    public class MemoryEfficientByteArrayPoolTests
    {
        [Fact]
        public void MemoryEfficientByteArrayPool_ShouldReuseArrays()
        {
            // Arrange
            var pool = new MemoryEfficientByteArrayPool(1024, 10, Mock.Of<ILogger>());

            // Act
            var array1 = pool.Rent();
            pool.Return(array1);
            var array2 = pool.Rent();

            // Assert
            Assert.Same(array1, array2);
        }
    }

    public class AzureKeyVaultManagerTests
    {
        private readonly Mock<KeyClient> _mockKeyClient;
        private readonly Mock<CryptographyClient> _mockCryptographyClient;
        private readonly AzureKeyVaultManager _keyVaultManager;

        //public AzureKeyVaultManagerTests()
        //{
        //    _mockKeyClient = new Mock<KeyClient>();
        //    _mockCryptographyClient = new Mock<CryptographyClient>();

        //    var mockOptions = new Mock<IOptions<AzureKeyVaultOptions>>();
        //    mockOptions.Setup(m => m.Value).Returns(new AzureKeyVaultOptions { KeyVaultUrl = "https://test.vault.azure.net/" });

        //    _keyVaultManager = new AzureKeyVaultManager(mockOptions.Object, Mock.Of<ILogger>());
        //    // Assuming the AzureKeyVaultManager constructor or another method should set the KeyClient and CryptographyClient
        //    // If not, you need to add methods to set these clients in the AzureKeyVaultManager class
        //    _keyVaultManager.SetKeyClient(_mockKeyClient.Object); // This line will cause an error if SetKeyClient method does not exist
        //    _keyVaultManager.SetCryptographyClient(_mockCryptographyClient.Object); // This line will cause an error if SetCryptographyClient method does not exist
        //}

        //[Fact]
        //public async Task GetEncryptionKeyAsync_ShouldReturnKey()
        //{
        //    // Arrange
        //    var keyName = "testKey";
        //    var keyValue = new byte[] { 1, 2, 3, 4, 5 };
        //    var keyBundle = KeyModelFactory.KeyVaultKey(new KeyVaultKey(keyName) { Key = new JsonWebKey(keyValue) });

        //    _mockKeyClient.Setup(m => m.GetKeyAsync(keyName, null, default))
        //        .ReturnsAsync(Response.FromValue(keyBundle, Mock.Of<Response>()));

        //    // Act
        //    var result = await _keyVaultManager.GetEncryptionKeyAsync(keyName);

        //    // Assert
        //    Assert.Equal(keyValue, result);
        //}

        [Fact]
        public async Task RotateKeyAsync_ShouldCallCreateKey()
        {
            // Arrange
            var keyName = "testKey";

            // Act
            await _keyVaultManager.RotateKeyAsync(keyName);

            // Assert
            _mockKeyClient.Verify(m => m.CreateKeyAsync(keyName, KeyType.Rsa, It.IsAny<CreateRsaKeyOptions>(), default), Times.Once);
        }
    }


    public class ErrorScenarioTests
    {
        private readonly Mock<IDistributedCacheProvider> _mockCacheProvider;
        private readonly MemStacheDistributed _memStache;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public ErrorScenarioTests()
        {
            _mockCacheProvider = new Mock<IDistributedCacheProvider>();
            var mockOptions = new Mock<IOptions<MemStacheOptions>>();
            mockOptions.Setup(m => m.Value).Returns(new MemStacheOptions());
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IDistributedCacheProvider))).Returns(_mockCacheProvider.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ISerializer))).Returns(new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions(), Mock.Of<ILogger>()));
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICompressor))).Returns(new GzipCompressor(Mock.Of<ILogger>()));
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IEncryptor))).Returns(new AesEncryptor(Mock.Of<ILogger>()));
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IKeyManager))).Returns(new AzureKeyVaultManager(Mock.Of<IOptions<AzureKeyVaultOptions>>(), Mock.Of<ILogger>()));

            _memStache = new MemStacheDistributed(
                new DistributedCacheProviderFactory(sp => _mockCacheProvider.Object),
                new SerializerFactory(sp => new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions(), Mock.Of<ILogger>())),
                new CompressorFactory(sp => new GzipCompressor(Mock.Of<ILogger>())),
                new EncryptorFactory(sp => new AesEncryptor(Mock.Of<ILogger>())),
                new KeyManagerFactory(sp => new AzureKeyVaultManager(Mock.Of<IOptions<AzureKeyVaultOptions>>(), Mock.Of<ILogger>())),
                mockOptions.Object,
                Mock.Of<ILogger>(),
                _mockServiceProvider.Object
            );
        }

        [Fact]
        public async Task GetAsync_ShouldThrowException_WhenNetworkFailure()
        {
            // Arrange
            _mockCacheProvider.Setup(m => m.GetAsync(It.IsAny<string>(), default))
                .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Simulated network failure"));

            // Act & Assert
            await Assert.ThrowsAsync<RedisConnectionException>(() => _memStache.GetAsync<string>("testKey"));
        }

        [Fact]
        public async Task SetAsync_ShouldThrowException_WhenTimeout()
        {
            // Arrange
            _mockCacheProvider.Setup(m => m.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<MemStacheEntryOptions>(), default))
                .ThrowsAsync(new TimeoutException("Simulated timeout"));

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => _memStache.SetAsync("testKey", "testValue"));
        }
    }



}
