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
using StackExchange.Redis;

namespace MemStache.Distributed.Tests.Unit
{
    public class ErrorScenarioTests
    {
        private readonly Mock<IDistributedCacheProvider> _mockCacheProvider;
        private readonly Mock<ISerializer> _mockSerializer;
        private readonly Mock<ICompressor> _mockCompressor;
        private readonly Mock<ICryptoService> _mockCryptoService;
        private readonly Mock<IKeyManagementService> _mockKeyManagementService;
        private readonly Mock<ILogger<MemStacheDistributed>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider; // Add this line
        private readonly MemStacheDistributed _memStache;

        public ErrorScenarioTests()
        {
            _mockCacheProvider = new Mock<IDistributedCacheProvider>();
            _mockSerializer = new Mock<ISerializer>();
            _mockCompressor = new Mock<ICompressor>();
            _mockCryptoService = new Mock<ICryptoService>();
            _mockKeyManagementService = new Mock<IKeyManagementService>();
            _mockLogger = new Mock<ILogger<MemStacheDistributed>>();
            _mockServiceProvider = new Mock<IServiceProvider>(); // Add this line

            var options = new MemStacheOptions
            {
                EnableCompression = true,
                EnableEncryption = true
            };
            var mockOptions = Mock.Of<IOptions<MemStacheOptions>>(m => m.Value == options);

            _memStache = new MemStacheDistributed(
                (Factories.DistributedCacheProviderFactory)_mockCacheProvider.Object,
                (Factories.SerializerFactory)_mockSerializer.Object,
                (Factories.CompressorFactory)_mockCompressor.Object,
                _mockCryptoService.Object,
                _mockKeyManagementService.Object,
                mockOptions,
                (Serilog.ILogger)_mockLogger.Object,
                _mockServiceProvider.Object // Add this line
            );
        }

        // ... (rest of the code remains unchanged)
    }
}
