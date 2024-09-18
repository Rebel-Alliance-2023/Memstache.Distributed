using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Serilog;
using MemStache.Distributed;
using MemStache.Distributed.KeyVaultManagement;
using MemStache.Distributed.Security;
using StackExchange.Redis;

namespace Memstache.Distributed.Tests.IntegrationTests
{
    public class MemStacheDerivedKeyIntegrationTests : IAsyncLifetime
    {
        private IServiceProvider _serviceProvider;
        private IMemStacheDistributed _memStache;
        private IKeyManagementService _keyManagementService;
        private ConnectionMultiplexer _redis;
        private readonly ILogger _serilogLogger;

        public MemStacheDerivedKeyIntegrationTests()
        {
            _serilogLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // Add logging with Serilog
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(_serilogLogger, dispose: true));

            // Configure Azure Key Vault (use emulator or real Key Vault)
            services.AddAzureKeyVault(options =>
            {
                options.KeyVaultUrl = "https://your-keyvault-url"; // Adjust as needed
            }, useEmulator: true);

            // Add Redis Cache configuration
            services.AddRedisCache(options =>
            {
                options.Configuration = "localhost:6379"; // Adjust as needed
            });

            // Register MemStache services
            services.AddMemStacheDistributed(options =>
            {
                options.DistributedCacheProvider = "Redis";
                options.Serializer = "SystemTextJson";
                options.Compressor = "Gzip";
                options.EnableCompression = true;
                options.EnableEncryption = true;
            });

            _serviceProvider = services.BuildServiceProvider();

            // Resolve services
            _memStache = _serviceProvider.GetRequiredService<IMemStacheDistributed>();
            _keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();

            // Initialize Redis connection
            _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");

            // Initialize master key
            await InitializeKeys();
        }

        private async Task InitializeKeys()
        {
            // Generate master key
            await _keyManagementService.GenerateMasterKeyAsync();
            await _keyManagementService.GenerateDerivedKeyAsync("test-key");
        }

        [Fact]
        public async Task SetAndGetUsingDerivedKey_ShouldStoreAndRetrieveCorrectValue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Generate a derived key
            var derivedKey = await _keyManagementService.GenerateDerivedKeyAsync(key);

            // Act
            await _memStache.SetAsync(key, value);
            string? retrievedValue = await _memStache.GetAsync<string>(key);

            // Assert
            Assert.Equal(value, retrievedValue);
        }

        public async Task DisposeAsync()
        {
            // Clean up Redis
            //if (_redis != null)
            //{
            //    var endpoints = _redis.GetEndPoints();
            //    foreach (var endpoint in endpoints)
            //    {
            //        var server = _redis.GetServer(endpoint);
            //        await server.FlushDatabaseAsync();
            //    }
            //    _redis.Dispose();
            //}
        }
    }
}
