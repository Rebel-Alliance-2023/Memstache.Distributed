using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using Serilog;
using MemStache.Distributed;
using MemStache.Distributed.Factories;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Encryption;
using MemStache.Distributed.KeyVaultManagement;
using MemStache.Distributed.Security;
using Rebel.Alliance.KeyVault.Secrets.Emulator;
using NBitcoin.JsonConverters;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Azure.Identity;

namespace Tests.MemStache.Distributed
{
    public class MemStacheIntegrationTests : IAsyncLifetime
    {
        private IServiceProvider _serviceProvider;
        private ICryptoService _cryptoService;
        private IKeyManagementService _keyManagementService;
        private IMemStacheDistributed _memStache;
        private IAzureKeyVaultSecretsWrapper _keyVaultSecretsWrapper;
        private ConnectionMultiplexer _redis;
        private readonly Serilog.ILogger _serilogLogger;

        public MemStacheIntegrationTests()
        {
            _serilogLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var services = new ServiceCollection();

                // Add logging with Serilog
                services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(_serilogLogger, dispose: true));

                // Use the extension methods to configure services with the emulator
                services.AddAzureKeyVault(options =>
                {
                    options.KeyVaultUrl = "https://your-keyvault-url"; // Ensure this is a valid URI
                }, useEmulator: true);

                // Add Redis Cache configuration
                services.AddRedisCache(options =>
                {
                    options.Configuration = "localhost:6379"; // Using Redis on Docker
                });

                // Register all MemStache services using the extension method
                services.AddMemStacheDistributed(options =>
                {
                    options.DistributedCacheProvider = "Redis";
                    options.Serializer = "SystemTextJson";
                    options.Compressor = "Gzip";
                });

                _serviceProvider = services.BuildServiceProvider();

                // Resolve services
                _cryptoService = _serviceProvider.GetRequiredService<ICryptoService>();
                _keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();
                _memStache = _serviceProvider.GetRequiredService<IMemStacheDistributed>();
                _keyVaultSecretsWrapper = _serviceProvider.GetRequiredService<IAzureKeyVaultSecretsWrapper>();

                // Initialize Redis connection
                _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "Error during InitializeAsync");
                throw;
            }
        }




        [Fact]
        public async Task SetAndGetString_ShouldStoreAndRetrieveCorrectValue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Act
            await _memStache.SetAsync<string>(key, value);
            string? retrievedValue =  await _memStache.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task DeleteKey_ShouldRemoveKey()
        {
            string key = "test_key_delete";
            string value = "value_to_delete";

            // Set a value in Redis
            await _memStache!.SetAsync(key, value);

            // Delete the key
            await _memStache.RemoveAsync(key);

            // Ensure the key no longer exists
            string? actualValue = await _memStache.GetAsync<string>(key);
            Assert.Null(actualValue);
        }

        [Fact]
        public async Task ExpireKey_ShouldRemoveKeyAfterTTL()
        {
            string key = "test_key_ttl";
            string value = "value_with_ttl";

            // Set a value with a TTL (Time-To-Live)
            await _memStache!.SetAsync(key, value, new MemStacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(1) });

            // Wait for TTL to expire
            await Task.Delay(2000);

            // Ensure the key no longer exists
            string? actualValue = await _memStache.GetAsync<string>(key);
            Assert.Null(actualValue);
        }

        public Task DisposeAsync()
        {
            // Clean up Redis by flushing the database
            //_redis!.GetServer("localhost", 6379).FlushDatabase();
            _redis.Dispose();
            return Task.CompletedTask;
        }
    }
}
