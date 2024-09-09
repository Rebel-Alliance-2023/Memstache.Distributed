# MemStache.Distributed Library Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Core Components](#core-components)
3. [Configuration](#configuration)
4. [Caching Operations](#caching-operations)
5. [Serialization](#serialization)
6. [Compression](#compression)
7. [Encryption](#encryption)
8. [Distributed Cache Providers](#distributed-cache-providers)
9. [Key Management](#key-management)
10. [Eviction Policies](#eviction-policies)
11. [Multi-tenancy Support](#multi-tenancy-support)
12. [Performance Optimizations](#performance-optimizations)
13. [Logging and Telemetry](#logging-and-telemetry)
14. [Resilience](#resilience)
15. [Cache Warm-up and Seeding](#cache-warm-up-and-seeding)
16. [Usage Examples](#usage-examples)
17. [Testing](#testing)

## 1. Introduction

MemStache.Distributed is a high-performance, distributed caching library for .NET applications. It provides a flexible, modular, and extensible caching solution with strong security features and support for various distributed caching providers.

Key features:
- Distributed caching with support for multiple providers (e.g., Redis)
- Data compression and encryption
- Flexible serialization options
- Configurable eviction policies
- Multi-tenancy support
- Performance optimizations
- Logging and telemetry integration
- Resilience patterns implementation

## 2. Core Components

The library consists of several core components:

- `IMemStacheDistributed`: The main interface for interacting with the cache.
- `MemStacheDistributed`: The primary implementation of the caching functionality.
- `IDistributedCacheProvider`: Interface for distributed cache providers.
- `ISerializer`: Interface for data serialization.
- `ICompressor`: Interface for data compression.
- `IEncryptor`: Interface for data encryption.
- `IKeyManager`: Interface for encryption key management.
- `IEvictionPolicy`: Interface for cache eviction policies.

## 3. Configuration

Configuration is done using the `MemStacheOptions` class:

```csharp
public class MemStacheOptions
{
    public string DistributedCacheProvider { get; set; } = "Redis";
    public string KeyManagementProvider { get; set; } = "AzureKeyVault";
    public string Serializer { get; set; } = "SystemTextJson";
    public bool EnableCompression { get; set; } = true;
    public bool EnableEncryption { get; set; } = true;
    public EvictionPolicy GlobalEvictionPolicy { get; set; } = EvictionPolicy.LRU;
    public TimeSpan? DefaultAbsoluteExpiration { get; set; }
    public TimeSpan? DefaultSlidingExpiration { get; set; }
}
```

## 4. Caching Operations

The `IMemStacheDistributed` interface provides the following operations:

- `GetAsync<T>(string key)`: Retrieve a value from the cache.
- `SetAsync<T>(string key, T value, MemStacheEntryOptions options)`: Store a value in the cache.
- `RemoveAsync(string key)`: Remove a value from the cache.
- `ExistsAsync(string key)`: Check if a key exists in the cache.
- `TryGetAsync<T>(string key)`: Attempt to retrieve a value from the cache.

## 5. Serialization

The default serializer uses `System.Text.Json`. The `SystemTextJsonSerializer` class implements the `ISerializer` interface:

```csharp
public interface ISerializer
{
    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[] data);
}
```

## 6. Compression

Compression is handled by the `GzipCompressor` class, which implements the `ICompressor` interface:

```csharp
public interface ICompressor
{
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] compressedData);
}
```

## 7. Encryption

Encryption is managed by the `AesEncryptor` class, implementing the `IEncryptor` interface:

```csharp
public interface IEncryptor
{
    byte[] Encrypt(byte[] data, byte[] key);
    byte[] Decrypt(byte[] encryptedData, byte[] key);
}
```

## 8. Distributed Cache Providers

The initial implementation includes a Redis cache provider (`RedisDistributedCacheProvider`). It implements the `IDistributedCacheProvider` interface:

```csharp
public interface IDistributedCacheProvider
{
    Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, byte[] value, MemStacheEntryOptions options, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
```

## 9. Key Management

Key management is handled by the `AzureKeyVaultManager` class, which implements the `IKeyManager` interface:

```csharp
public interface IKeyManager
{
    Task<byte[]> GetEncryptionKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default);
    Task RotateKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default);
}
```

## 10. Eviction Policies

The library supports multiple eviction policies:

- LRU (Least Recently Used)
- LFU (Least Frequently Used)
- Time-based

Each policy implements the `IEvictionPolicy` interface:

```csharp
public interface IEvictionPolicy
{
    void RecordAccess(string key);
    string SelectVictim();
}
```

## 11. Multi-tenancy Support

Multi-tenancy is supported through the `TenantManager` class, which wraps the `IMemStacheDistributed` interface and prefixes keys with a tenant identifier.

## 12. Performance Optimizations

Performance optimizations include:

- `BatchOperationManager`: Handles batching of cache operations.
- `MemoryEfficientByteArrayPool`: Provides efficient byte array pooling.

## 13. Logging and Telemetry

The library integrates with Serilog for logging and OpenTelemetry for distributed tracing and metrics.

## 14. Resilience

Resilience patterns are implemented using the Polly library, including:

- Circuit breaker
- Retry policies

## 15. Cache Warm-up and Seeding

The `CacheWarmer` class provides functionality to pre-populate the cache on startup or during off-peak hours.

## 16. Usage Examples

Here's a basic example of how to use MemStache.Distributed:

```csharp
services.AddMemStacheDistributed(options =>
{
    options.DistributedCacheProvider = "Redis";
    options.KeyManagementProvider = "AzureKeyVault";
    options.EnableEncryption = true;
    options.GlobalEvictionPolicy = EvictionPolicy.LRU;
})
.AddRedisCache(redisOptions => { /* Redis-specific options */ })
.AddAzureKeyVault(keyVaultOptions => { /* Azure Key Vault options */ });

// In a controller or service
public class MyService
{
    private readonly IMemStacheDistributed _cache;

    public MyService(IMemStacheDistributed cache)
    {
        _cache = cache;
    }

    public async Task<MyData> GetDataAsync(string key)
    {
        return await _cache.GetAsync<MyData>(key) 
            ?? await FetchAndCacheDataAsync(key);
    }

    private async Task<MyData> FetchAndCacheDataAsync(string key)
    {
        var data = await FetchDataFromSourceAsync();
        await _cache.SetAsync(key, data, new MemStacheEntryOptions
        {
            AbsoluteExpiration = TimeSpan.FromHours(1),
            Compress = true,
            Encrypt = true
        });
        return data;
    }
}
```

## 17. Testing

The library includes extensive unit tests and integration tests covering:

- Core caching operations
- Serialization and compression with various data types and sizes
- Encryption and key management
- Eviction policies
- Multi-tenancy
- Performance optimizations
- Error scenarios and resilience

Tests are implemented using xUnit and include mocks for external dependencies like Redis and Azure Key Vault.

---

This documentation provides an overview of the MemStache.Distributed library's functionality. For more detailed information on specific components or usage patterns, please refer to the inline documentation in the source code or reach out to the library maintainers.
