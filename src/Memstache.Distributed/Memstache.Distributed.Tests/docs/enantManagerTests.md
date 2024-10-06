# TenantManagerTests Explanation

This document provides a detailed, line-by-line explanation of the `TenantManagerTests` class, which contains unit tests for the `TenantManager` in the MemStache.Distributed library's multi-tenancy feature.

## Class Setup

```csharp
public class TenantManagerTests
{
    private readonly Mock<IMemStacheDistributed> _mockBaseCache;
    private readonly TenantManager _tenantManager;
    private readonly Mock<ILogger<TenantManager>> _mockLogger;
```

- The class declares private fields for:
  - A mock `IMemStacheDistributed` object (the base cache)
  - The `TenantManager` instance being tested
  - A mock `ILogger<TenantManager>` for logging

## Constructor

```csharp
public TenantManagerTests()
{
    _mockBaseCache = new Mock<IMemStacheDistributed>();
    _mockLogger = new Mock<ILogger<TenantManager>>();
    _tenantManager = new TenantManager(_mockBaseCache.Object, () => "tenant1", _mockLogger.Object);
}
```

The constructor sets up the test environment:
1. It creates a mock `IMemStacheDistributed` object.
2. It creates a mock `ILogger<TenantManager>` object.
3. It initializes the `TenantManager` with:
   - The mocked base cache
   - A function that always returns "tenant1" as the tenant ID
   - The mocked logger

## Test: GetAsync_ShouldPrefixKeyWithTenantId

```csharp
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
```

This test verifies that `GetAsync` correctly prefixes the key with the tenant ID:
1. It sets up the mock base cache to return an expected value for the tenant-prefixed key.
2. It calls `GetAsync` on the `TenantManager` with a test key.
3. It asserts that the returned value matches the expected value, implying that the tenant-prefixed key was used.

## Test: SetAsync_ShouldPrefixKeyWithTenantId

```csharp
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
```

This test ensures that `SetAsync` correctly prefixes the key with the tenant ID when setting a value:
1. It calls `SetAsync` on the `TenantManager` with a test key and value.
2. It verifies that the base cache's `SetAsync` method was called once with the correct tenant-prefixed key.

## Test: RemoveAsync_ShouldPrefixKeyWithTenantId

```csharp
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
```

This test verifies that `RemoveAsync` correctly prefixes the key with the tenant ID when removing a value:
1. It calls `RemoveAsync` on the `TenantManager` with a test key.
2. It verifies that the base cache's `RemoveAsync` method was called once with the correct tenant-prefixed key.

## Test: ExistsAsync_ShouldPrefixKeyWithTenantId

```csharp
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
```

This test ensures that `ExistsAsync` correctly prefixes the key with the tenant ID when checking for a key's existence:
1. It sets up the mock base cache to return true for the tenant-prefixed key.
2. It calls `ExistsAsync` on the `TenantManager` with a test key.
3. It asserts that the result is true, implying that the tenant-prefixed key was used in the check.

## Conclusion

The `TenantManagerTests` class provides comprehensive testing for the `TenantManager` class:

1. It verifies that all key-based operations (`GetAsync`, `SetAsync`, `RemoveAsync`, `ExistsAsync`) correctly prefix the key with the tenant ID.
2. It ensures that the `TenantManager` correctly delegates to the base cache after applying the tenant prefix.
3. It implicitly tests that the tenant ID provider function is correctly used in all operations.

These tests help ensure the reliability and correctness of the multi-tenancy feature in the MemStache.Distributed library. They verify that each tenant's data is properly isolated by key prefixing, which is crucial for maintaining data separation in a multi-tenant environment.

The use of mocking allows these tests to focus solely on the `TenantManager`'s behavior, isolating it from the actual implementation of the distributed cache. This approach provides a clear and focused set of tests for the multi-tenancy functionality.
