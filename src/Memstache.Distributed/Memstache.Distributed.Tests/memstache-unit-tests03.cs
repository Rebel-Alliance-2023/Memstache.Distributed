using System;
using System.Collections.Generic;
using System.Linq;
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
using MemStache.Distributed.Performance;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

namespace MemStache.Distributed.Tests.Unit
{
    // Existing tests here...

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

        public AzureKeyVaultManagerTests()
        {
            _mockKeyClient = new Mock<KeyClient>();
            _mockCryptographyClient = new Mock<CryptographyClient>();

            var mockOptions = new Mock<IOptions<AzureKeyVaultOptions>>();
            mockOptions.Setup(m => m.Value).Returns(new AzureKeyVaultOptions { KeyVaultUrl = "https://test.vault.azure.net/" });

            _keyVaultManager = new AzureKeyVaultManager(mockOptions.Object, Mock.Of<ILogger>());
            _keyVaultManager.SetKeyClient(_mockKeyClient.Object);
            _keyVaultManager.SetCryptographyClient(_mockCryptographyClient.Object);
        }

        [Fact]
        public async Task GetEncryptionKeyAsync_ShouldReturnKey()
        {
            // Arrange
            var keyName = "testKey";
            var keyValue = new byte[] { 1, 2, 3, 4, 5 };
            var keyBundle = KeyModelFactory.JsonWebKey(new JsonWebKey(keyValue));

            _mockKeyClient.Setup(m => m.GetKeyAsync(keyName, null, default))
                .ReturnsAsync(Response.FromValue(keyBundle, Mock.Of<Response>()));

            // Act
            var result = await _keyVaultManager.GetEncryptionKeyAsync(keyName);

            // Assert
            Assert.Equal(keyValue, result);
        }

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

        public ErrorScenarioTests()
        {
            _mockCacheProvider = new Mock<IDistributedCacheProvider>();
            var mockOptions = new Mock<IOptions<MemStacheOptions>>();
            mockOptions.Setup(m => m.Value).Returns(new MemStacheOptions());

            _memStache = new MemStacheDistributed(
                new DistributedCacheProviderFactory(sp => _mockCacheProvider.Object),
                new SerializerFactory(sp => new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions(), Mock.Of<ILogger>())),
                new CompressorFactory(sp => new GzipCompressor(Mock.Of<ILogger>())),
                new EncryptorFactory(sp => new AesEncryptor(Mock.Of<ILogger>())),
                new KeyManagerFactory(sp => new AzureKeyVaultManager(Mock.Of<IOptions<AzureKeyVaultOptions>>(), Mock.Of<ILogger>())),
                mockOptions.Object,
                Mock.Of<ILogger>()
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
