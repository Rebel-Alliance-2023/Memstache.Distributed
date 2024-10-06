# MemStacheDistributedCacheAdapter Documentation

## Overview

The `MemStacheDistributedCacheAdapter` is a crucial component in the MemStache.Distributed library that bridges the gap between the `IMemStacheDistributed` interface and the standard .NET `IDistributedCache` interface. This adapter allows MemStache.Distributed to be used as a drop-in replacement for any system expecting an `IDistributedCache` implementation while still providing access to the advanced features of `IMemStacheDistributed`.

## Class Definition

```csharp
public class MemStacheDistributedCacheAdapter : IMemStacheDistributed, IDistributedCache
```

The adapter implements both `IMemStacheDistributed` and `IDistributedCache` interfaces, allowing it to be used in contexts requiring either interface.

## Constructor

```csharp
public MemStacheDistributedCacheAdapter(IMemStacheDistributed memStacheDistributed)
```

- **Parameters:**
  - `memStacheDistributed`: An instance of `IMemStacheDistributed` that this adapter will wrap and delegate operations to.

## IDistributedCache Implementation

### Get

```csharp
public byte[]? Get(string key)
```

Retrieves a value from the cache synchronously.

- **Parameters:**
  - `key`: A string identifying the requested value.
- **Returns:** The cached value as a byte array, or null if the key is not found.

### GetAsync

```csharp
public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
```

Retrieves a value from the cache asynchronously.

- **Parameters:**
  - `key`: A string identifying the requested value.
  - `token`: Optional. A `CancellationToken` to cancel the operation.
- **Returns:** A task that represents the asynchronous operation, containing the cached value as a byte array, or null if the key is not found.

### Set

```csharp
public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
```

Sets a value in the cache synchronously.

- **Parameters:**
  - `key`: A string identifying the value.
  - `value`: The value to cache as a byte array.
  - `options`: The cache options for the value.

### SetAsync

```csharp
public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
```

Sets a value in the cache asynchronously.

- **Parameters:**
  - `key`: A string identifying the value.
  - `value`: The value to cache as a byte array.
  - `options`: The cache options for the value.
  - `token`: Optional. A `CancellationToken` to cancel the operation.
- **Returns:** A task that represents the asynchronous operation.

### Refresh

```csharp
public void Refresh(string key)
```

Refreshes a value in the cache synchronously, resetting its sliding expiration timeout.

- **Parameters:**
  - `key`: A string identifying the value to refresh.

### RefreshAsync

```csharp
public Task RefreshAsync(string key, CancellationToken token = default)
```

Refreshes a value in the cache asynchronously, resetting its sliding expiration timeout.

- **Parameters:**
  - `key`: A string identifying the value to refresh.
  - `token`: Optional. A `CancellationToken` to cancel the operation.
- **Returns:** A task that represents the asynchronous operation.

### Remove

```csharp
public void Remove(string key)
```

Removes a value from the cache synchronously.

- **Parameters:**
  - `key`: A string identifying the value to remove.

### RemoveAsync

```csharp
public Task RemoveAsync(string key, CancellationToken token = default)
```

Removes a value from the cache asynchronously.

- **Parameters:**
  - `key`: A string identifying the value to remove.
  - `token`: Optional. A `CancellationToken` to cancel the operation.
- **Returns:** A task that represents the asynchronous operation.

## IMemStacheDistributed Implementation

The adapter implements all methods of the `IMemStacheDistributed` interface by delegating to the wrapped `IMemStacheDistributed` instance. This includes methods for working with `Stash<T>` and `SecureStash<T>` objects, as well as advanced operations like `TryGetAsync`.

For full details on these methods, refer to the `IMemStacheDistributed` interface documentation.

## Key Features

1. **Dual Interface Implementation:** By implementing both `IDistributedCache` and `IMemStacheDistributed`, the adapter provides flexibility in usage across different parts of an application.

2. **Transparent Delegation:** Most operations are delegated to the underlying `IMemStacheDistributed` instance, ensuring that the full capabilities of MemStache.Distributed are available.

3. **Options Conversion:** The adapter handles the conversion between `DistributedCacheEntryOptions` and `MemStacheEntryOptions`, including the conversion of relative expiration times to absolute expiration times.

4. **Asynchronous Support:** All operations are available in both synchronous and asynchronous versions, with the synchronous versions internally using the asynchronous implementations.

## Usage Example

```csharp
// In your dependency injection setup
services.AddSingleton<IMemStacheDistributed, MemStacheDistributed>();
services.AddSingleton<IDistributedCache, MemStacheDistributedCacheAdapter>();

// In your application code
public class MyService
{
    private readonly IDistributedCache _cache;

    public MyService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GetValueAsync(string key)
    {
        var bytes = await _cache.GetAsync(key);
        return bytes != null ? Encoding.UTF8.GetString(bytes) : null;
    }

    public Task SetValueAsync(string key, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        return _cache.SetAsync(key, bytes, options);
    }
}
```

This example demonstrates how to use the `MemStacheDistributedCacheAdapter` as an `IDistributedCache` implementation in a typical service class.

## Conclusion

The `MemStacheDistributedCacheAdapter` provides a seamless way to integrate MemStache.Distributed into applications and frameworks that expect an `IDistributedCache` implementation. By wrapping an `IMemStacheDistributed` instance, it offers the best of both worlds: compatibility with standard .NET caching interfaces and access to the advanced features of MemStache.Distributed when needed.
