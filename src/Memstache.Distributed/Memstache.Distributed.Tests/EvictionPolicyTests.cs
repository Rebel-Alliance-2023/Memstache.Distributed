using System;
using Xunit;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.EvictionPolicies;

namespace MemStache.Distributed.Tests.Unit
{
    public class EvictionPolicyTests : IDisposable
    {
        private readonly Serilog.Core.Logger _serilogLogger;
        private readonly ILogger<LruEvictionPolicy> _lruLogger;
        private readonly ILogger<LfuEvictionPolicy> _lfuLogger;
        private readonly ILogger<TimeBasedEvictionPolicy> _timeBasedLogger;
        private readonly ITestOutputHelper _output;

        public EvictionPolicyTests(ITestOutputHelper output)
        {
            _output = output;

            // Configure Serilog to write to the xUnit test output
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(_output) // Direct Serilog logs to xUnit output
                .WriteTo.Console() // Also write to the console
                .CreateLogger();

            // Use Serilog's LoggerFactory to create Microsoft.Extensions.Logging.ILogger instances
            var loggerFactory = new SerilogLoggerFactory(_serilogLogger);
            _lruLogger = loggerFactory.CreateLogger<LruEvictionPolicy>();
            _lfuLogger = loggerFactory.CreateLogger<LfuEvictionPolicy>();
            _timeBasedLogger = loggerFactory.CreateLogger<TimeBasedEvictionPolicy>();
        }

        [Fact]
        public void LruEvictionPolicy_ShouldEvictLeastRecentlyUsedItem()
        {
            // Arrange
            var policy = new LruEvictionPolicy(_serilogLogger);

            // Act
            policy.RecordAccess("key1");
            policy.RecordAccess("key2");
            policy.RecordAccess("key3");
            policy.RecordAccess("key1");

            // Assert
            Assert.Equal("key2", policy.SelectVictim());
        }

        [Fact]
        public void LfuEvictionPolicy_ShouldEvictLeastFrequentlyUsedItem()
        {
            // Arrange
            var policy = new LfuEvictionPolicy(_serilogLogger);

            // Act
            policy.RecordAccess("key1");
            policy.RecordAccess("key2");
            policy.RecordAccess("key3");
            policy.RecordAccess("key2");
            policy.RecordAccess("key3");
            policy.RecordAccess("key3");

            // Assert
            Assert.Equal("key1", policy.SelectVictim());
        }

        [Fact]
        public void TimeBasedEvictionPolicy_ShouldEvictExpiredItem()
        {
            // Arrange
            var policy = new TimeBasedEvictionPolicy(_serilogLogger);

            // Act
            policy.SetExpiration("key1", DateTime.UtcNow.AddSeconds(1));
            policy.SetExpiration("key2", DateTime.UtcNow.AddSeconds(2));
            policy.SetExpiration("key3", DateTime.UtcNow.AddSeconds(-1)); // Already expired

            // Assert
            Assert.Equal("key3", policy.SelectVictim());
        }

        [Fact]
        public void TimeBasedEvictionPolicy_ShouldReturnNullWhenNoExpiredItems()
        {
            // Arrange
            var policy = new TimeBasedEvictionPolicy(_serilogLogger);

            // Act
            policy.SetExpiration("key1", DateTime.UtcNow.AddSeconds(1));
            policy.SetExpiration("key2", DateTime.UtcNow.AddSeconds(2));

            // Assert
            Assert.Null(policy.SelectVictim());
        }

        // Dispose of the logger properly
        public void Dispose()
        {
            _serilogLogger?.Dispose();
        }
    }
}
