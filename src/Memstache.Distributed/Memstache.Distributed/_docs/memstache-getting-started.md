# Getting Started with MemStache.Distributed

This guide will walk you through the process of integrating MemStache.Distributed into your .NET project, from installation to basic usage.

## Installation

1. Add the MemStache.Distributed NuGet package to your project:

   ```shell
   dotnet add package MemStache.Distributed
   ```

2. For local development, also add the Rebel Alliance KeyVault Secrets Emulator:

   ```shell
   dotnet add package Rebel.Alliance.KeyVault.Secrets.Emulator
   ```

3. Ensure you have a Redis instance available. For local development, you can use Docker:

   ```shell
   docker run --name redis -p 6379:6379 -d redis
   ```

## Basic Configuration

Configure MemStache.Distributed in your `Program.cs` or `Startup.cs` file:

```csharp
using MemStache.Distributed;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Determine if we're in a development environment
    bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

    // Configure Azure Key Vault (or emulator for development)
    services.AddAzureKeyVault(options =>
    {
        options.KeyVaultUrl = isDevelopment 
            ? "http://localhost:5000" // Emulator URL
            : "https://your-azure-keyvault-url";
    }, useEmulator: isDevelopment);

    // Configure Redis
    services.AddRedisCache(options =>
    {
        options.Configuration = "localhost:6379";
    });

    // Configure MemStache
    services.AddMemStacheDistributed(options =>
    {
        options.DistributedCacheProvider = "Redis";
        options.Serializer = "SystemTextJson";
        options.Compressor = "Gzip";
        options.EnableCompression = true;
        options.EnableEncryption = true;
        options.GlobalEvictionPolicy = EvictionPolicy.LRU;
        options.KeyManagementProvider = "AzureKeyVault";
    });
}
```

## Quick Start Example

Here's a simple example demonstrating how to use MemStache.Distributed in your application:

```csharp
using MemStache.Distributed;
using Microsoft.Extensions.DependencyInjection;

public class ExampleService
{
    private readonly IMemStacheDistributed _cache;

    public ExampleService(IMemStacheDistributed cache)
    {
        _cache = cache;
    }

    public async Task<string> GetOrSetDataAsync(string key)
    {
        // Try to get the data from the cache
        var cachedData = await _cache.GetAsync<string>(key);

        if (cachedData != null)
        {
            return cachedData;
        }

        // If not in cache, generate the data
        var newData = $"Data for {key} generated at {DateTime.UtcNow}";

        // Set the data in the cache with a 5-minute expiration
        await _cache.SetAsync(key, newData, new MemStacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5)
        });

        return newData;
    }
}

// In your Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // ... (previous MemStache configuration)

    services.AddScoped<ExampleService>();
}

// Usage in a controller or another service
public class ExampleController : ControllerBase
{
    private readonly ExampleService _exampleService;

    public ExampleController(ExampleService exampleService)
    {
        _exampleService = exampleService;
    }

    [HttpGet("data/{key}")]
    public async Task<IActionResult> GetData(string key)
    {
        var data = await _exampleService.GetOrSetDataAsync(key);
        return Ok(data);
    }
}
```

This example demonstrates how to inject and use `IMemStacheDistributed` in a service to get or set data in the cache.

## Next Steps

- Explore more advanced features like [Secure Stash](./SecureStash.md) for handling sensitive data.
- Learn about [Multi-tenancy Support](./MultiTenancy.md) if you're building a multi-tenant application.
- Check out the [Performance Optimization](./Performance.md) guide for tips on getting the most out of MemStache.Distributed.

With these steps, you should have MemStache.Distributed up and running in your .NET application. The library is now ready to provide efficient distributed caching with built-in security features, leveraging Redis for storage and Azure KeyVault (or its emulator) for key management.

