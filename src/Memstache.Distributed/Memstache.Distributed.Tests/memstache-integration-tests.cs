using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MemStache.Distributed;
using StackExchange.Redis;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Serilog;

namespace MemStache.Distributed.Tests.Integration
{
    public class MemStacheDistributedIntegrationTests : IAsyncLifetime
    {
        private IServiceProvider _serviceProvider;
        private IMemStacheDistributed _memStache;
        private ConnectionMultiplexer _redis;
        private KeyClient _keyVaultClient;

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            // Configure MemStache
            services.AddMemStacheDistributed(options =>
            {
                options.DistributedCacheProvider = "Redis";
                options.KeyManagementProvider = "AzureKeyVault";
                options.EnableCompression = true;
                options.EnableEncryption = true;
            });

            services.AddRedisCache(options =>
            {
                options.Configuration = "localhost:6379"; // Ensure Redis is running on this address
            });

            services.AddAzureKeyVault(options =>
            {
                options.KeyVaultUrl = "https://your-key-vault.vault.azure.net/"; // Replace with your Key Vault URL
            });

            _serviceProvider = services.BuildServiceProvider();
            _memStache = _serviceProvider.GetRequiredService<IMemStacheDistributed>();

            // Initialize Redis connection
            _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");

            // Initialize Azure Key Vault client
            _keyVaultClient = new KeyClient(new Uri("https://your-key-vault.vault.azure.net/"), new DefaultAzureCredential());
        }

        public async Task DisposeAsync()
        {
            _redis.Dispose();
            await _keyVaultClient.DisposeAsync();
        }

        [Fact]
        public async Task SetAndGetAsync_ShouldStoreAndRetrieveValue()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";

            // Act
            await _memStache.SetAsync(key, value);
            var retrievedValue = await _memStache.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveValue()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            await _memStache.SetAsync(key, value);

            // Act
            await _memStache.RemoveAsync(key);
            var exists = await _memStache.ExistsAsync(key);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrueForExistingKey()
        {
            // Arrange
            var key = "testKey";
            var value = "testValue";
            await _memStache.SetAsync(key, value);

            // Act
            var exists = await _memStache.ExistsAsync(key);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task SetAsync_WithExpiration_ShouldExpireAfterSpecifiedTime()
        {
            // Arrange
            var key = "expiringKey";
            var value = "expiringValue";
            var options = new MemStacheEntryOptions
            {
                AbsoluteExpiration = TimeSpan.FromSeconds(1)
            };

            // Act
            await _memStache.SetAsync(key, value, options);
            await Task.Delay(1500); // Wait for expiration
            var exists = await _memStache.ExistsAsync(key);

            // Assert
            Assert.False(exists);
        }

        // Add more integration tests...
    }
}
