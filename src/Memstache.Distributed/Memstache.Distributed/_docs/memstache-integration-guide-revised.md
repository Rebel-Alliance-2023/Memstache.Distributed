# Using MemStache in a .NET Project with KeyStore Integration

MemStache.Distributed offers seamless integration with .NET applications using dependency injection, with a particular emphasis on secure key management through Azure KeyVault integration. For local development and testing, MemStache provides integration with the Rebel Alliance KeyVault Secrets Emulator, ensuring a consistent development experience without the need for a live Azure KeyVault instance.

## Key Features

- Azure KeyVault integration for secure key management in production
- Rebel Alliance KeyVault Secrets Emulator support for local development and testing
- Flexible configuration options for different environments

## Prerequisites

Before integrating MemStache, ensure you have:

- A .NET project targeting .NET 8.0 or later
- Access to a Redis instance (local or remote)
- Azure KeyVault access for production environments
- [Rebel Alliance KeyVault Secrets Emulator](https://github.com/Rebel-Alliance-2023/Rebel.Alliance.KeyVault.Secrets.Emulator) for local development

## Installation

Add the MemStache.Distributed NuGet package to your project:

```shell
dotnet add package MemStache.Distributed
```

For local development, also add the Rebel Alliance KeyVault Secrets Emulator:

```shell
dotnet add package Rebel.Alliance.KeyVault.Secrets.Emulator
```

## Configuring Services

Here's a step-by-step guide to setting up MemStache with KeyStore integration:

1. **Add Logging**

   Configure Serilog for logging:

   ```csharp
   services.AddLogging(loggingBuilder => 
       loggingBuilder.AddSerilog(Log.Logger, dispose: true));
   ```

2. **Configure KeyStore Integration**

   This is a critical step that sets up the secure key management for MemStache. You can configure it to use either Azure KeyVault or the Rebel Alliance KeyVault Secrets Emulator:

   ```csharp
   services.AddAzureKeyVault(options =>
   {
       options.KeyVaultUrl = "https://your-keyvault-url";
   }, useEmulator: isDevelopment); // Set to true for local development with emulator
   ```

   The `useEmulator` parameter is crucial here. When set to `true`, MemStache will use the Rebel Alliance KeyVault Secrets Emulator instead of attempting to connect to a real Azure KeyVault. This allows for seamless development and testing without the need for a live Azure KeyVault instance.

3. **Configure Redis Cache**

   Set up the Redis connection:

   ```csharp
   services.AddRedisCache(options =>
   {
       options.Configuration = "your-redis-connection-string";
   });
   ```

4. **Register MemStache Services**

   Use the `AddMemStacheDistributed` extension method to register all MemStache services:

   ```csharp
   services.AddMemStacheDistributed(options =>
   {
       options.DistributedCacheProvider = "Redis";
       options.Serializer = "SystemTextJson";
       options.Compressor = "Gzip";
       options.EnableCompression = true;
       options.EnableEncryption = true;
       options.GlobalEvictionPolicy = EvictionPolicy.LRU;
       options.KeyManagementProvider = "AzureKeyVault"; // This setting works with both real KeyVault and the emulator
   });
   ```

   Note the `KeyManagementProvider` option, which is set to use Azure KeyVault. This setting works with both the real Azure KeyVault and the Rebel Alliance KeyVault Secrets Emulator, depending on the `useEmulator` parameter set in step 2.

5. **Build Service Provider**

   After configuring all services, build the service provider:

   ```csharp
   var serviceProvider = services.BuildServiceProvider();
   ```

## Resolving and Using MemStache Services

Once configured, resolve and use MemStache services:

```csharp
var memStache = serviceProvider.GetRequiredService<IMemStacheDistributed>();
var cryptoService = serviceProvider.GetRequiredService<ICryptoService>();
var keyManagementService = serviceProvider.GetRequiredService<IKeyManagementService>();
var keyVaultSecretsWrapper = serviceProvider.GetRequiredService<IAzureKeyVaultSecretsWrapper>();
```

Note the `IAzureKeyVaultSecretsWrapper` service, which provides a unified interface for interacting with both Azure KeyVault and the Rebel Alliance KeyVault Secrets Emulator.

## Initializing the Master Key

Initialize a master key for encryption:

```csharp
private async Task InitializeMasterKey()
{
    var keyManagementService = serviceProvider.GetRequiredService<IKeyManagementService>();
    MasterKey masterKey = await keyManagementService.GenerateMasterKeyAsync();
}
```

This operation will use either Azure KeyVault or the emulator, depending on your configuration.

## Complete Setup Example

Here's a complete example showcasing the KeyStore integration:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

    services.AddLogging(loggingBuilder => 
        loggingBuilder.AddSerilog(Log.Logger, dispose: true));

    // KeyStore Configuration
    services.AddAzureKeyVault(options =>
    {
        options.KeyVaultUrl = isDevelopment ? "http://localhost:5000" : "https://your-azure-keyvault-url";
    }, useEmulator: isDevelopment);

    services.AddRedisCache(options =>
    {
        options.Configuration = "localhost:6379";
    });

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

public async Task InitializeAsync()
{
    var serviceProvider = services.BuildServiceProvider();
    var memStache = serviceProvider.GetRequiredService<IMemStacheDistributed>();
    var keyManagementService = serviceProvider.GetRequiredService<IKeyManagementService>();
    var keyVaultSecretsWrapper = serviceProvider.GetRequiredService<IAzureKeyVaultSecretsWrapper>();

    await InitializeMasterKey(keyManagementService);

    // Your application is now ready to use MemStache with secure key management
}

private async Task InitializeMasterKey(IKeyManagementService keyManagementService)
{
    MasterKey masterKey = await keyManagementService.GenerateMasterKeyAsync();
}
```

This setup ensures that your application uses Azure KeyVault in production environments while seamlessly switching to the Rebel Alliance KeyVault Secrets Emulator for local development and testing. This approach provides a consistent and secure key management experience across all stages of your application lifecycle.

