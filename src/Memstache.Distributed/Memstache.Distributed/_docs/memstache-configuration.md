# MemStache.Distributed Configuration Guide

Proper configuration is crucial for getting the most out of MemStache.Distributed. This guide covers the main configuration areas: MemStacheOptions, Redis configuration, and Azure Key Vault integration.

## MemStacheOptions

MemStacheOptions is the primary configuration class for MemStache.Distributed. It allows you to customize various aspects of the caching behavior.

### Available Options

```csharp
public class MemStacheOptions
{
    public string DistributedCacheProvider { get; set; } = "Redis";
    public string KeyManagementProvider { get; set; } = "AzureKeyVault";
    public string Serializer { get; set; } = "SystemTextJson";
    public string Compressor { get; set; }
    public bool EnableCompression { get; set; } = true;
    public bool EnableEncryption { get; set; } = true;
    public EvictionPolicy GlobalEvictionPolicy { get; set; } = EvictionPolicy.LRU;
    public TimeSpan? DefaultAbsoluteExpiration { get; set; }
    public TimeSpan? DefaultSlidingExpiration { get; set; }
}
```

### Configuration Example

Here's how to configure MemStacheOptions when setting up the service:

```csharp
services.AddMemStacheDistributed(options =>
{
    options.DistributedCacheProvider = "Redis";
    options.KeyManagementProvider = "AzureKeyVault";
    options.Serializer = "SystemTextJson";
    options.Compressor = "Gzip";
    options.EnableCompression = true;
    options.EnableEncryption = true;
    options.GlobalEvictionPolicy = EvictionPolicy.LRU;
    options.DefaultAbsoluteExpiration = TimeSpan.FromHours(1);
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(10);
});
```

### Option Details

- **DistributedCacheProvider**: Specifies the underlying cache provider. Currently, only "Redis" is supported.
- **KeyManagementProvider**: Defines the key management service. "AzureKeyVault" is the default and recommended option.
- **Serializer**: Sets the serialization method. "SystemTextJson" is the default.
- **Compressor**: Specifies the compression algorithm. "Gzip" is recommended when compression is enabled.
- **EnableCompression**: Toggles data compression. Enabled by default to reduce storage and bandwidth usage.
- **EnableEncryption**: Toggles data encryption. Enabled by default for enhanced security.
- **GlobalEvictionPolicy**: Sets the default eviction policy for the cache.
- **DefaultAbsoluteExpiration**: Sets a default absolute expiration time for cache entries.
- **DefaultSlidingExpiration**: Sets a default sliding expiration time for cache entries.

## Redis Configuration

MemStache.Distributed uses Redis as its distributed cache provider. Proper Redis configuration is essential for optimal performance.

### RedisOptions

```csharp
public class RedisOptions
{
    public string Configuration { get; set; }
    public int Database { get; set; } = -1;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public bool AllowAdmin { get; set; } = false;
    public string Password { get; set; }
    public string[] EndPoints { get; set; }
    public bool Ssl { get; set; } = false;
    public string SslHost { get; set; }
}
```

### Configuration Example

Here's how to configure Redis when setting up MemStache:

```csharp
services.AddRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.Database = 0;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 5000;
    options.AllowAdmin = false;
    options.Password = "your-redis-password";
    options.Ssl = true;
    options.SslHost = "your-redis-ssl-host";
});
```

### Option Details

- **Configuration**: The Redis connection string.
- **Database**: The Redis database index to use. -1 (default) uses the default database.
- **ConnectTimeout**: The timeout for connect operations (milliseconds).
- **SyncTimeout**: The timeout for synchronous operations (milliseconds).
- **AllowAdmin**: Whether to allow admin operations.
- **Password**: The password for the Redis server.
- **EndPoints**: An array of Redis server endpoints for clustering.
- **Ssl**: Whether to use SSL for the connection.
- **SslHost**: The SSL host name.

## Azure Key Vault Integration

MemStache.Distributed integrates with Azure Key Vault for secure key management in production environments.

### AzureKeyVaultOptions

```csharp
public class AzureKeyVaultOptions
{
    public string KeyVaultUrl { get; set; }
}
```

### Configuration Example

Here's how to configure Azure Key Vault integration:

```csharp
services.AddAzureKeyVault(options =>
{
    options.KeyVaultUrl = "https://your-keyvault.vault.azure.net/";
}, useEmulator: false);
```

### Rebel Alliance KeyVault Secrets Emulator

For local development, you can use the Rebel Alliance KeyVault Secrets Emulator:

```csharp
services.AddAzureKeyVault(options =>
{
    options.KeyVaultUrl = "http://localhost:5000";
}, useEmulator: true);
```

### Authentication

MemStache.Distributed uses the DefaultAzureCredential for authentication with Azure Key Vault. This allows for a variety of authentication methods:

1. Environment variables
2. Managed Identity
3. Visual Studio authentication
4. Azure CLI authentication
5. Interactive browser authentication

Ensure that your application has the necessary permissions to access the Key Vault.

### Key Rotation

MemStache.Distributed supports automatic key rotation. To implement key rotation:

1. Set up a schedule for key rotation (e.g., using a background service).
2. Call the `RotateMasterKeyAsync` method on the `IKeyManagementService`:

```csharp
public class KeyRotationService : BackgroundService
{
    private readonly IKeyManagementService _keyManagementService;

    public KeyRotationService(IKeyManagementService keyManagementService)
    {
        _keyManagementService = keyManagementService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _keyManagementService.RotateMasterKeyAsync();
            await Task.Delay(TimeSpan.FromDays(30), stoppingToken); // Rotate every 30 days
        }
    }
}
```

By properly configuring MemStacheOptions, Redis, and Azure Key Vault integration, you can ensure that MemStache.Distributed operates efficiently and securely in your application. Remember to adjust these settings based on your specific requirements and environment.

