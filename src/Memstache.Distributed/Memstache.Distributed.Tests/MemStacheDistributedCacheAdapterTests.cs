using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using MemStache.Distributed;
using MemStache.Distributed.Adapters;
using Serilog;
using Serilog.Events;
using MemStache.Distributed.Secure;

namespace Memstache.Distributed.Tests
{
    public class MemStacheDistributedCacheAdapterTests : IDisposable
    {
        private readonly Mock<IMemStacheDistributed> _mockMemStacheDistributed;
        private readonly MemStacheDistributedCacheAdapter _adapter;
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly ITestOutputHelper _output;

        public MemStacheDistributedCacheAdapterTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            _mockMemStacheDistributed = new Mock<IMemStacheDistributed>();
            _adapter = new MemStacheDistributedCacheAdapter(_mockMemStacheDistributed.Object);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnValueFromMemStacheDistributed()
        {
            // Arrange
            var key = "testKey";
            var expectedValue = new byte[] { 1, 2, 3 };
            _mockMemStacheDistributed.Setup(m => m.TryGetAsync<byte[]>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedValue, true));

            // Act
            var result = await _adapter.GetAsync(key);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Fact]
        public async Task SetAsync_ShouldCallMemStacheDistributedWithCorrectParameters()
        {
            // Arrange
            var key = "testKey";
            var value = new byte[] { 1, 2, 3 };
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            // Act
            await _adapter.SetAsync(key, value, options);

            // Assert
            _mockMemStacheDistributed.Verify(m => m.SetAsync(
                key,
                value,
                It.Is<MemStacheEntryOptions>(o => o.AbsoluteExpiration.HasValue),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_ShouldGetAndSetValue()
        {
            // Arrange
            var key = "testKey";
            var value = new byte[] { 1, 2, 3 };
            _mockMemStacheDistributed.Setup(m => m.TryGetAsync<byte[]>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((value, true));

            // Act
            await _adapter.RefreshAsync(key);

            // Assert
            _mockMemStacheDistributed.Verify(m => m.TryGetAsync<byte[]>(key, It.IsAny<CancellationToken>()), Times.Once);
            _mockMemStacheDistributed.Verify(m => m.SetAsync(key, value, It.IsAny<MemStacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_ShouldCallMemStacheDistributedRemove()
        {
            // Arrange
            var key = "testKey";

            // Act
            await _adapter.RemoveAsync(key);

            // Assert
            _mockMemStacheDistributed.Verify(m => m.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetStashAsync_ShouldDelegateToMemStacheDistributed()
        {
            // Arrange
            var key = "testKey";
            var expectedStash = new Stash<string>(key, "testValue");
            _mockMemStacheDistributed.Setup(m => m.GetStashAsync<string>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStash);

            // Act
            var result = await _adapter.GetStashAsync<string>(key);

            // Assert
            Assert.Equal(expectedStash, result);
        }

        [Fact]
        public async Task SetSecureStashAsync_ShouldDelegateToMemStacheDistributed()
        {
            // Arrange
            var key = "testKey";
            var secureStash = new SecureStash<string>(null, null); // Assuming a constructor that takes ICryptoService and IKeyManagementService
            secureStash.Key = key;

            // Act
            await _adapter.SetSecureStashAsync(secureStash);

            // Assert
            _mockMemStacheDistributed.Verify(m => m.SetSecureStashAsync(secureStash, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
