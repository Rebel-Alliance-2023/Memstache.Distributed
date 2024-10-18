# MemStache.Distributed

MemStache.Distributed is a high-performance, feature-rich distributed caching library for .NET applications. It provides a robust and secure solution for managing distributed caches, offering seamless integration with popular technologies and a focus on developer productivity.

## Features

- **Distributed Caching**: Leverages Redis for high-performance data storage and retrieval across multiple application instances.
- **Secure Key Management**: Integrates with Azure Key Vault and supports the Rebel Alliance KeyVault Secrets Emulator for local development.
- **Data Protection**: Built-in encryption and compression capabilities.
- **Multi-tenancy Support**: Efficient cache isolation for multi-tenant applications.
- **Flexible Serialization**: Supports various serialization options.
- **Advanced Eviction Policies**: Implements LRU, LFU, and time-based eviction strategies.
- **Performance Optimizations**: Includes batch operations and memory-efficient byte array pooling.
- **Resilience and Error Handling**: Implements circuit breaker and retry policies.
- **Telemetry and Logging**: Integrates with popular logging and monitoring solutions.
- **Extensibility**: Designed for easy customization and extension.

## Documentation

For more detailed information, please refer to the following documentation:

- [Introduction](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/edit/main/src/Memstache.Distributed/Memstache.Distributed/readme.md)
- [Getting Started](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-getting-started.md) 
- [Core Concepts](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-core-concepts.md)
- [Features in Detail](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-features-in-detail.md)
- [Configuration](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-configuration.md)
- [Usage Guide](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-usage-guide.md)
- [Advanced Topics](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-advanced-topics.md)
- [Development and Testing](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-development-and-testing.md)
- [API Reference](https://github.com/Rebel-Alliance-2023/Memstache.Distributed/blob/main/src/Memstache.Distributed/Memstache.Distributed/_docs/memstache-api-reference.md)

## Quick Start

1. Install the NuGet package:
   ```
   dotnet add package MemStache.Distributed
   ```

2. Configure MemStache in your `Startup.cs` or `Program.cs`:
   ```csharp
   services.AddMemStacheDistributed(options =>
   {
       options.DistributedCacheProvider = "Redis";
       options.EnableCompression = true;
       options.EnableEncryption = true;
   });
   ```

3. Inject and use `IMemStacheDistributed` in your classes:
   ```csharp
   public class MyService
   {
       private readonly IMemStacheDistributed _cache;

       public MyService(IMemStacheDistributed cache)
       {
           _cache = cache;
       }

       public async Task<string> GetValueAsync(string key)
       {
           return await _cache.GetAsync<string>(key);
       }
   }
   ```

For more detailed usage instructions, see the [Usage Guide](/_docs/UsageGuide.md).

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](/_docs/DevelopmentAndTesting.md#contributing-guidelines) for more information on how to get started.

## License

MemStache.Distributed is released under the MIT License. See the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/yourusername/MemStache.Distributed/issues) on our GitHub repository.

