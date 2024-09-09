using System;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MemStache.Distributed.EvictionPolicies;

namespace MemStache.Distributed.Tests.Unit
{
    public class EvictionPolicyTests
    {
        [Fact]
        public void LruEvictionPolicy_ShouldEvictLeastRecentlyUsedItem()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LruEvictionPolicy>>();
            var policy = new LruEvictionPolicy((Serilog.ILogger)mockLogger.Object);
            
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
            var mockLogger = new Mock<ILogger<LfuEvictionPolicy>>();
            var policy = new LfuEvictionPolicy((Serilog.ILogger)mockLogger.Object);
            
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
            var mockLogger = new Mock<ILogger<TimeBasedEvictionPolicy>>();
            var policy = new TimeBasedEvictionPolicy((Serilog.ILogger)mockLogger.Object);
            
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
            var mockLogger = new Mock<ILogger<TimeBasedEvictionPolicy>>();
            var policy = new TimeBasedEvictionPolicy((Serilog.ILogger)mockLogger.Object);
            
            // Act
            policy.SetExpiration("key1", DateTime.UtcNow.AddSeconds(1));
            policy.SetExpiration("key2", DateTime.UtcNow.AddSeconds(2));

            // Assert
            Assert.Null(policy.SelectVictim());
        }
    }
}
