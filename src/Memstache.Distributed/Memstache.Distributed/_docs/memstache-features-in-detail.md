# MemStache.Distributed Features in Detail

MemStache.Distributed offers a rich set of features designed to provide a secure, efficient, and scalable caching solution. This section delves into the details of these features, explaining how they work and how to leverage them in your applications.

## Encryption and Key Management

MemStache.Distributed implements robust encryption and key management to ensure the security of cached data.

### Encryption

- **Algorithm**: Uses AES encryption with RSA for key exchange.
- **Implementation**: Leverages the `ICryptoService` interface for encryption and decryption operations.

```csharp
public class CryptoService : ICryptoService
{
    public byte[] EncryptData(byte[] publicKey, byte[] data)
    {
        // Implementation details...
    }

    public byte[] DecryptData(byte[] privateKey, byte[] encryptedData)
    {
        // Implementation details...
    }
}
```

### Key Management

MemStache uses a hierarchical key structure with master and derived keys:

- **Master Keys**: Root keys used to generate derived keys.
- **Derived Keys**: Used for actual data encryption, rotated regularly for enhanced security.

Key management is handled by the `IKeyManagementService`:

```csharp
public interface IKeyManagementService
{
    Task<MasterKey> GenerateMasterKeyAsync();
    Task<DerivedKey> GenerateDerivedKeyAsync(string masterKeyId = null);
    Task<MasterKey> GetMasterKeyAsync(string keyId = null);
    Task<DerivedKey> GetDerivedKeyAsync(string keyId = null);
    Task<MasterKey> RotateMasterKeyAsync(string masterKeyId = null);
}
```

### Azure Key Vault Integration

For production environments, MemStache integrates with Azure Key Vault for secure key storage:

```csharp
services.AddAzureKeyVault(options =>
{
    options.KeyVaultUrl = "https://your-keyvault-url";
});
```

### Rebel Alliance KeyVault Secrets Emulator

For local development, MemStache supports the Rebel Alliance KeyVault Secrets Emulator:

```csharp
services.AddAzureKeyVault(options =>
{
    options.KeyVaultUrl = "http://localhost:5000";
}, useEmulator: true);
```

## Compression

MemStache offers data compression to reduce storage requirements and network bandwidth usage.

- **Algorithm**: Uses GZip compression by default.
- **Implementation**: Through the `ICompressor` interface.

```csharp
public class GzipCompressor : ICompressor
{
    public byte[] Compress(byte[] data)
    {
        // Compression implementation...
    }

    public byte[] Decompress(byte[] compressedData)
    {
        // Decompression implementation...
    }
}
```

Enable compression in the configuration:

```csharp
services.AddMemStacheDistributed(options =>
{
    options.EnableCompression = true;
    options.Compressor = "Gzip";
});
```

## Multi-tenancy Support

MemStache provides built-in support for multi-tenant applications, allowing efficient cache isolation between tenants.

Key features:

- **Tenant-specific key prefixing**: Automatically prefixes cache keys with the tenant identifier.
- **Tenant resolution**: Flexible tenant identification through a customizable tenant resolver.

Example configuration:

```csharp
services.AddMemStacheMultiTenancy(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return () => httpContextAccessor.HttpContext?.User.FindFirst("TenantId")?.Value;
});
```

Usage remains the same as single-tenant scenarios, with MemStache handling tenant isolation transparently:

```csharp
await _memStache.SetAsync("myKey", myValue);
var value = await _memStache.GetAsync<MyType>("myKey");
```

## Performance Optimizations

MemStache incorporates several performance optimizations:

### Batch Operations

The `BatchOperationManager` allows for efficient handling of multiple cache operations:

```csharp
public class BatchOperationManager<TKey, TResult>
{
    public Task<TResult> GetOrAddAsync(TKey key, Func<TKey, Task<TResult>> operation)
    {
        // Implementation details...
    }
}
```

### Memory-Efficient Byte Array Pooling

Reduces garbage collection pressure through efficient byte array reuse:

```csharp
public class MemoryEfficientByteArrayPool
{
    public byte[] Rent()
    {
        // Implementation details...
    }

    public void Return(byte[] array)
    {
        // Implementation details...
    }
}
```

## Resilience and Error Handling

MemStache implements robust error handling and resilience strategies:

### Circuit Breaker

Prevents cascading failures by temporarily disabling operations after a series of failures:

```csharp
private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

_circuitBreakerPolicy = Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );
```

### Retry Policies

Automatically retries failed operations with exponential backoff:

```csharp
private readonly AsyncRetryPolicy _retryPolicy;

_retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
    );
```

## Telemetry and Logging

MemStache provides comprehensive telemetry and logging capabilities:

### Logging

Utilizes Serilog for structured logging:

```csharp
services.AddLogging(loggingBuilder => 
    loggingBuilder.AddSerilog(Log.Logger, dispose: true));
```

### OpenTelemetry Integration

Supports OpenTelemetry for distributed tracing and metrics:

```csharp
services.AddMemStacheTelemetry(options =>
{
    options.UseJaeger = true;
    options.UseZipkin = false;
    options.UsePrometheus = true;
});
```

This integration allows for detailed monitoring of cache operations, performance metrics, and distributed tracing across your application.

By leveraging these advanced features, MemStache.Distributed provides a comprehensive caching solution that addresses security, performance, scalability, and observability needs in modern distributed applications. Each feature can be fine-tuned to meet specific requirements, allowing for a highly customized caching strategy tailored to your application's needs.

