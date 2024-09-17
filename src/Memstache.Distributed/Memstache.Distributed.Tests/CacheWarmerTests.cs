using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Warmup;
using Moq;

namespace MemStache.Distributed.Tests.Unit
{
    public class CacheWarmerTests : IDisposable
    {
        private readonly Mock<IMemStacheDistributed> _mockCache;
        private readonly Mock<ICacheSeeder> _mockSeeder1;
        private readonly Mock<ICacheSeeder> _mockSeeder2;
        private readonly CacheWarmer _cacheWarmer;
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly ITestOutputHelper _output;

        public CacheWarmerTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            // Initialize the mock objects
            _mockCache = new Mock<IMemStacheDistributed>();
            _mockSeeder1 = new Mock<ICacheSeeder>();
            _mockSeeder2 = new Mock<ICacheSeeder>();

            var seeders = new List<ICacheSeeder> { _mockSeeder1.Object, _mockSeeder2.Object };

            // Use Serilog logger directly for CacheWarmer
            _cacheWarmer = new CacheWarmer(_mockCache.Object, seeders, _serilogLogger);
        }

        [Fact]
        public async Task StartAsync_ShouldCallSeedCacheAsyncOnAllSeeders()
        {
            // Arrange
            _mockSeeder1.Setup(s => s.SeedCacheAsync(_mockCache.Object, default)).Returns(Task.CompletedTask);
            _mockSeeder2.Setup(s => s.SeedCacheAsync(_mockCache.Object, default)).Returns(Task.CompletedTask);

            // Act
            await _cacheWarmer.StartAsync(default);

            // Assert
            _mockSeeder1.Verify(s => s.SeedCacheAsync(_mockCache.Object, default), Times.Once);
            _mockSeeder2.Verify(s => s.SeedCacheAsync(_mockCache.Object, default), Times.Once);
        }

        [Fact]
        public async Task StartAsync_ShouldContinueExecutionIfOneSeederFails()
        {
            // Arrange
            _mockSeeder1.Setup(s => s.SeedCacheAsync(_mockCache.Object, default)).ThrowsAsync(new Exception("Seeding failed"));
            _mockSeeder2.Setup(s => s.SeedCacheAsync(_mockCache.Object, default)).Returns(Task.CompletedTask);

            // Act
            await _cacheWarmer.StartAsync(default);

            // Assert
            _mockSeeder1.Verify(s => s.SeedCacheAsync(_mockCache.Object, default), Times.Once);
            _mockSeeder2.Verify(s => s.SeedCacheAsync(_mockCache.Object, default), Times.Once);
            _serilogLogger.Error(It.IsAny<Exception>(), "Error during cache seeding");
        }

        [Fact]
        public async Task StopAsync_ShouldCompleteSuccessfully()
        {
            // Act
            await _cacheWarmer.StopAsync(default);

            // Assert
            // StopAsync is expected to complete without throwing an exception
            Assert.True(true);
        }

        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
