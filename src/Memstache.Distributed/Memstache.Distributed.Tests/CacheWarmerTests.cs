using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.Warmup;

namespace MemStache.Distributed.Tests.Unit
{
    public class CacheWarmerTests
    {
        private readonly Mock<IMemStacheDistributed> _mockCache;
        private readonly Mock<ICacheSeeder> _mockSeeder1;
        private readonly Mock<ICacheSeeder> _mockSeeder2;
        private readonly Mock<ILogger<CacheWarmer>> _mockLogger;
        private readonly CacheWarmer _cacheWarmer;

        public CacheWarmerTests()
        {
            _mockCache = new Mock<IMemStacheDistributed>();
            _mockSeeder1 = new Mock<ICacheSeeder>();
            _mockSeeder2 = new Mock<ICacheSeeder>();
            _mockLogger = new Mock<ILogger<CacheWarmer>>();
            
            var seeders = new List<ICacheSeeder> { _mockSeeder1.Object, _mockSeeder2.Object };
            _cacheWarmer = new CacheWarmer(_mockCache.Object, seeders, (Serilog.ILogger)_mockLogger.Object);
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
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error during cache seeding")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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
    }
}
