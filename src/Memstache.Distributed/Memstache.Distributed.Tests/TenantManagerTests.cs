using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.MultiTenancy;

namespace MemStache.Distributed.Tests.Unit
{
    public class TenantManagerTests
    {
        private readonly Mock<IMemStacheDistributed> _mockBaseCache;
        private readonly TenantManager _tenantManager;
        private readonly Mock<ILogger<TenantManager>> _mockLogger;

        public TenantManagerTests()
        {
            _mockBaseCache = new Mock<IMemStacheDistributed>();
            _mockLogger = new Mock<ILogger<TenantManager>>();
            _tenantManager = new TenantManager(_mockBaseCache.Object, () => "tenant1", _mockLogger.Object);
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

        [Fact]
        public async Task RemoveAsync_ShouldPrefixKeyWithTenantId()
        {
            // Arrange
            var key = "testKey";

            // Act
            await _tenantManager.RemoveAsync(key);

            // Assert
            _mockBaseCache.Verify(m => m.RemoveAsync("tenant1:testKey", default), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_ShouldPrefixKeyWithTenantId()
        {
            // Arrange
            var key = "testKey";
            _mockBaseCache.Setup(m => m.ExistsAsync("tenant1:testKey", default)).ReturnsAsync(true);

            // Act
            var result = await _tenantManager.ExistsAsync(key);

            // Assert
            Assert.True(result);
        }
    }
}
