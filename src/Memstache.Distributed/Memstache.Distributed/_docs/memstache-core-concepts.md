# Core Concepts of MemStache.Distributed

Understanding the core concepts behind MemStache.Distributed is crucial for effectively utilizing the library in your applications. This section covers the fundamental principles of distributed caching, the Stash and SecureStash abstractions, and the eviction policies implemented in MemStache.Distributed.

## Distributed Caching

Distributed caching is a technique used to store and retrieve data across multiple servers or instances of an application. It offers several benefits over local in-memory caching:

1. **Scalability**: Distributed caches can grow horizontally by adding more servers to the cache cluster.
2. **High Availability**: Data is replicated across multiple nodes, ensuring availability even if some nodes fail.
3. **Consistency**: All instances of an application access the same cached data, ensuring consistency across the system.
4. **Performance**: By reducing the load on backend systems and minimizing network requests, distributed caching can significantly improve application performance.

MemStache.Distributed uses Redis as its underlying distributed cache provider. Redis is an open-source, in-memory data structure store that excels at caching scenarios due to its speed and versatility.

### Key Features of MemStache's Distributed Caching:

- **Redis Integration**: Seamless integration with Redis for robust and scalable caching.
- **Serialization**: Automatic serialization and deserialization of complex objects.
- **Compression**: Optional compression to reduce network bandwidth and storage requirements.
- **Encryption**: Built-in encryption capabilities to secure sensitive cached data.

## Stash and SecureStash

MemStache.Distributed introduces two important abstractions for working with cached data: Stash and SecureStash.

### Stash

A Stash represents a cached item along with its metadata. It provides a rich and flexible caching mechanism that goes beyond simple key-value pairs.

Key components of a Stash:

- **Key**: A unique identifier for the cached item.
- **Value**: The actual data being cached.
- **StoredType**: The type of the stored data, allowing for type-safe retrieval.
- **Size**: The size of the cached item, useful for cache management.
- **Hash**: A hash of the data, which can be used for integrity checks.
- **ExpirationDate**: When the cached item should expire.
- **Plan**: An enum indicating how the data should be processed (e.g., serialization, compression, encryption).

Example usage of Stash:

```csharp
var stash = new Stash<MyObject>(
    key: "myKey",
    value: myObject,
    plan: StashPlan.CompressAndEncrypt
);
await memStache.SetStashAsync(stash);
```

### SecureStash

SecureStash extends the concept of Stash by adding encryption capabilities. It's designed for storing sensitive data that requires an extra layer of security.

Additional features of SecureStash:

- **EncryptionKeyId**: An identifier for the key used to encrypt the data.
- **EncryptedData**: The encrypted form of the data.
- **Encryption and Decryption Methods**: Built-in methods to handle the secure transformation of data.

Example usage of SecureStash:

```csharp
var secureStash = new SecureStash<SensitiveData>(keyManagementService, cryptoService)
{
    Key = "sensitiveKey",
    Value = sensitiveData
};
await secureStash.EncryptAsync();
await memStache.SetSecureStashAsync(secureStash);
```

## Eviction Policies

Eviction policies are strategies used to manage the cache when it reaches its capacity limit. MemStache.Distributed implements several eviction policies to suit different caching needs:

1. **Least Recently Used (LRU)**:
   - Removes the least recently accessed items first.
   - Ideal for scenarios where recently accessed items are likely to be accessed again.

2. **Least Frequently Used (LFU)**:
   - Removes the least frequently accessed items first.
   - Suitable for scenarios where access frequency is a good predictor of future accesses.

3. **Time-Based**:
   - Removes items based on their expiration time.
   - Useful for scenarios where data freshness is critical.

4. **Custom**:
   - Allows implementation of custom eviction strategies tailored to specific application needs.

Example of setting a global eviction policy:

```csharp
services.AddMemStacheDistributed(options =>
{
    options.GlobalEvictionPolicy = EvictionPolicy.LRU;
});
```

You can also set eviction policies on a per-item basis using `MemStacheEntryOptions`:

```csharp
await memStache.SetAsync("myKey", myValue, new MemStacheEntryOptions
{
    AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
    SlidingExpiration = TimeSpan.FromMinutes(10)
});
```

Understanding these core concepts - distributed caching, Stash and SecureStash, and eviction policies - provides a solid foundation for effectively using MemStache.Distributed in your applications. These features work together to provide a powerful, flexible, and secure caching solution that can significantly enhance your application's performance and scalability.

