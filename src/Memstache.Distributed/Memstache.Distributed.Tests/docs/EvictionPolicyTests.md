# EvictionPolicyTests Explanation

This document provides a detailed, line-by-line explanation of the `EvictionPolicyTests` class, which contains unit tests for various eviction policies in the MemStache.Distributed library.

## Class Setup

```csharp
public class EvictionPolicyTests : IDisposable
{
    private readonly Serilog.Core.Logger _serilogLogger;
    private readonly ILogger<LruEvictionPolicy> _lruLogger;
    private readonly ILogger<LfuEvictionPolicy> _lfuLogger;
    private readonly ILogger<TimeBasedEvictionPolicy> _timeBasedLogger;
    private readonly ITestOutputHelper _output;
```

- The class implements `IDisposable` to properly clean up resources.
- It declares private fields for:
  - A Serilog logger
  - Three Microsoft.Extensions.Logging.ILogger instances, one for each eviction policy
  - An xUnit `ITestOutputHelper` for test output

## Constructor

```csharp
public EvictionPolicyTests(ITestOutputHelper output)
{
    _output = output;

    _serilogLogger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.TestOutput(_output)
        .WriteTo.Console()
        .CreateLogger();

    var loggerFactory = new SerilogLoggerFactory(_serilogLogger);
    _lruLogger = loggerFactory.CreateLogger<LruEvictionPolicy>();
    _lfuLogger = loggerFactory.CreateLogger<LfuEvictionPolicy>();
    _timeBasedLogger = loggerFactory.CreateLogger<TimeBasedEvictionPolicy>();
}
```

- The constructor takes an `ITestOutputHelper` parameter for xUnit test output.
- It configures a Serilog logger to write to both the xUnit test output and the console.
- It creates a `SerilogLoggerFactory` to create Microsoft.Extensions.Logging.ILogger instances for each eviction policy.

## Test: LruEvictionPolicy_ShouldEvictLeastRecentlyUsedItem

```csharp
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
```

This test verifies that the Least Recently Used (LRU) eviction policy correctly selects the least recently used item:
- It creates an instance of `LruEvictionPolicy`.
- It records access to keys in a specific order, with "key1" being accessed twice.
- It asserts that "key2" is selected as the victim for eviction, as it was the least recently used.

## Test: LfuEvictionPolicy_ShouldEvictLeastFrequentlyUsedItem

```csharp
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
```

This test ensures that the Least Frequently Used (LFU) eviction policy correctly selects the least frequently used item:
- It creates an instance of `LfuEvictionPolicy`.
- It records access to keys with varying frequencies.
- It asserts that "key1" is selected as the victim for eviction, as it was accessed only once.

## Test: TimeBasedEvictionPolicy_ShouldEvictExpiredItem

```csharp
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
```

This test verifies that the Time-Based eviction policy correctly selects an expired item:
- It creates an instance of `TimeBasedEvictionPolicy`.
- It sets expiration times for three keys, with "key3" having an expiration time in the past.
- It asserts that "key3" is selected as the victim for eviction, as it has already expired.

## Test: TimeBasedEvictionPolicy_ShouldReturnNullWhenNoExpiredItems

```csharp
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
```

This test ensures that the Time-Based eviction policy returns null when no items have expired:
- It creates an instance of `TimeBasedEvictionPolicy`.
- It sets future expiration times for two keys.
- It asserts that `SelectVictim()` returns null, as no items have expired.

## Cleanup

```csharp
public void Dispose()
{
    _serilogLogger?.Dispose();
}
```

The `Dispose` method ensures that the Serilog logger is properly disposed of after the tests are complete.

## Conclusion

This test suite provides coverage for three different eviction policies:
1. Least Recently Used (LRU)
2. Least Frequently Used (LFU)
3. Time-Based

Each test verifies the core functionality of its respective eviction policy, ensuring that:
- LRU correctly identifies the least recently used item
- LFU correctly identifies the least frequently used item
- Time-Based correctly identifies expired items and handles cases where no items have expired

These tests help ensure the reliability and correctness of the eviction policies in the MemStache.Distributed library, which are crucial for managing cache size and relevance.
