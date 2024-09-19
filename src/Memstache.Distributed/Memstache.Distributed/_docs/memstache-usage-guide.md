# MemStache.Distributed Usage Guide

This guide provides practical examples and best practices for using MemStache.Distributed in your .NET applications. We'll cover basic operations, working with Stash objects, implementing SecureStash for sensitive data, and leveraging multi-tenancy.

## Basic Operations

MemStache.Distributed provides a simple interface for basic cache operations through the `IMemStacheDistributed` interface.

### Dependency Injection

First, inject `IMemStacheDistributed` into your class:

```csharp
public class MyService
{
    private readonly IMemStacheDistributed _cache;

    public MyService(IMemStacheDistributed cache)
    {
        _cache = cache;
    }

    // ... other methods
}
```

### Get Operation

Retrieve an item from the cache:

```csharp
public async Task<string> GetUserNameAsync(int userId)
{
    string cacheKey = $"user:{userId}:name";
    string userName = await _cache.GetAsync<string>(cacheKey);

    if (userName == null)
    {
        // Cache miss: fetch from database and cache
        userName = await _userRepository.GetUserNameAsync(userId);
        await _cache.SetAsync(cacheKey, userName, new MemStacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
        });
    }

    return userName;
}
```

### Set Operation

Store an item in the cache:

```csharp
public async Task UpdateUserProfileAsync(UserProfile profile)
{
    await _userRepository.UpdateUserProfileAsync(profile);

    string cacheKey = $"user:{profile.UserId}:profile";
    await _cache.SetAsync(cacheKey, profile, new MemStacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    });
}
```

### Remove Operation

Remove an item from the cache:

```csharp
public async Task InvalidateUserCacheAsync(int userId)
{
    string[] cacheKeys = new[]
    {
        $"user:{userId}:name",
        $"user:{userId}:profile"
    };

    foreach (var key in cacheKeys)
    {
        await _cache.RemoveAsync(key);
    }
}
```

## Working with Stash Objects

Stash objects provide a richer caching mechanism with additional metadata and processing options.

### Creating and Setting a Stash

```csharp
public async Task CacheComplexDataAsync(string key, ComplexData data)
{
    var stash = new Stash<ComplexData>(key, data)
    {
        ExpirationDate = DateTime.UtcNow.AddDays(1),
        Plan = StashPlan.CompressAndEncrypt
    };

    await _cache.SetStashAsync(stash, new MemStacheEntryOptions
    {
        AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
    });
}
```

### Retrieving a Stash

```csharp
public async Task<ComplexData> GetComplexDataAsync(string key)
{
    var stash = await _cache.GetStashAsync<ComplexData>(key);

    if (stash != null)
    {
        // Access stash metadata
        Console.WriteLine($"Data size: {stash.Size} bytes");
        Console.WriteLine($"Expiration: {stash.ExpirationDate}");

        return stash.Value;
    }

    return null; // or fetch from the original source
}
```

### Using TryGetStashAsync for Better Performance

```csharp
public async Task<ComplexData> GetComplexDataEfficientlyAsync(string key)
{
    var (stash, success) = await _cache.TryGetStashAsync<ComplexData>(key);

    if (success)
    {
        return stash.Value;
    }

    // Cache miss: fetch and cache
    var data = await FetchComplexDataFromSourceAsync(key);
    await CacheComplexDataAsync(key, data);
    return data;
}
```

## Implementing SecureStash for Sensitive Data

SecureStash provides an additional layer of security for sensitive data by handling encryption and decryption.

### Creating and Setting a SecureStash

```csharp
public class SecureDataService
{
    private readonly IMemStacheDistributed _cache;
    private readonly IKeyManagementService _keyManagementService;
    private readonly ICryptoService _cryptoService;

    public SecureDataService(IMemStacheDistributed cache, IKeyManagementService keyManagementService, ICryptoService cryptoService)
    {
        _cache = cache;
        _keyManagementService = keyManagementService;
        _cryptoService = cryptoService;
    }

    public async Task CacheSecureDataAsync(string key, SensitiveData data)
    {
        var secureStash = new SecureStash<SensitiveData>(_keyManagementService, _cryptoService)
        {
            Key = key,
            Value = data
        };

        await secureStash.EncryptAsync();
        await _cache.SetSecureStashAsync(secureStash, new MemStacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
        });
    }

    public async Task<SensitiveData> GetSecureDataAsync(string key)
    {
        var secureStash = await _cache.GetSecureStashAsync<SensitiveData>(key);

        if (secureStash != null)
        {
            await secureStash.DecryptAsync();
            return secureStash.Value;
        }

        return null; // or fetch from the original source
    }
}
```

### Rotating Keys for SecureStash

```csharp
public async Task RotateSecureDataKeyAsync(string key)
{
    var secureStash = await _cache.GetSecureStashAsync<SensitiveData>(key);

    if (secureStash != null)
    {
        await secureStash.RotateKeyAsync();
        await _cache.SetSecureStashAsync(secureStash);
    }
}
```

## Leveraging Multi-tenancy

MemStache.Distributed supports multi-tenancy out of the box. Once configured, it automatically handles tenant-specific caching.

### Configuring Multi-tenancy

In your `Startup.cs` or `Program.cs`:

```csharp
services.AddMemStacheMultiTenancy(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return () => httpContextAccessor.HttpContext?.User.FindFirst("TenantId")?.Value;
});
```

### Using Multi-tenant Cache

With multi-tenancy configured, you can use MemStache as usual. The tenant context is automatically applied:

```csharp
public class MultiTenantService
{
    private readonly IMemStacheDistributed _cache;

    public MultiTenantService(IMemStacheDistributed cache)
    {
        _cache = cache;
    }

    public async Task<TenantData> GetTenantDataAsync(string dataKey)
    {
        // The cache key is automatically prefixed with the current tenant ID
        return await _cache.GetAsync<TenantData>(dataKey);
    }

    public async Task SetTenantDataAsync(string dataKey, TenantData data)
    {
        // Data is automatically isolated per tenant
        await _cache.SetAsync(dataKey, data);
    }
}
```

### Accessing Data Across Tenants

In some cases, you might need to access or manage data across tenants. MemStache provides a way to bypass the automatic tenant prefixing:

```csharp
public class CrossTenantService
{
    private readonly IMemStacheDistributed _cache;

    public CrossTenantService(IMemStacheDistributed cache)
    {
        _cache = cache;
    }

    public async Task<GlobalConfig> GetGlobalConfigAsync()
    {
        // Use a special prefix to indicate a global (non-tenant-specific) key
        string globalKey = "global:config";
        return await _cache.GetAsync<GlobalConfig>(globalKey);
    }

    public async Task SetGlobalConfigAsync(GlobalConfig config)
    {
        string globalKey = "global:config";
        await _cache.SetAsync(globalKey, config);
    }
}
```

By following these patterns and best practices, you can effectively leverage the full power of MemStache.Distributed in your applications. Remember to handle cache misses gracefully, implement appropriate error handling, and consider the specific requirements of your application when choosing between different caching strategies.

