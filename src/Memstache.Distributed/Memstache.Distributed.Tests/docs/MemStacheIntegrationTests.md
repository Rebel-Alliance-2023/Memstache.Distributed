# MemStacheIntegrationTests Documentation

This document provides a detailed explanation of the `MemStacheIntegrationTests` class, which contains integration tests for the MemStache.Distributed library.

## Overview

The `MemStacheIntegrationTests` class is designed to test the integration of various components of the MemStache.Distributed library, including Redis caching, Azure Key Vault (or its emulator), and the core MemStache functionality. It sets up a complete environment to perform end-to-end tests of the MemStache system.

## Class Definition

```csharp
public class MemStacheIntegrationTests : IAsyncLifetime
```

The class implements `IAsyncLifetime`, which allows for asynchronous setup and teardown of the test environment.

## Fields

- `private IServiceProvider _serviceProvider`: The dependency injection container.
- `private ICryptoService _cryptoService`: The cryptographic service.
- `private IKeyManagementService _keyManagementService`: The key management service.
- `private IMemStacheDistributed _memStache`: The MemStache distributed cache instance.
- `private IAzureKeyVaultSecretsWrapper _keyVaultSecretsWrapper`: The Azure Key Vault secrets wrapper.
- `private ConnectionMultiplexer _redis`: The Redis connection.
- `private string _masterKeyId`: The ID of the master key (not used in the current implementation).
- `private readonly Serilog.ILogger _serilogLogger`: The Serilog logger.

## Constructor

```csharp
public MemStacheIntegrationTests()
```

The constructor initializes the Serilog logger for console output.

## Initialization

```csharp
public async Task InitializeAsync()
```

This method is called before each test runs. It sets up the entire test environment:

1. Configures dependency injection.
2. Sets up logging with Serilog.
3. Configures Azure Key Vault (using an emulator in this case).
4. Configures Redis cache.
5. Registers MemStache services with specific options.
6. Builds the service provider and resolves necessary services.
7. Initializes the Redis connection.
8. Initializes the master key.

### Master Key Initialization

```csharp
private async Task InitializeMasterKey()
```

This private method generates a master key using the key management service.

## Test Methods

### SetAndGetString_ShouldStoreAndRetrieveCorrectValue

```csharp
[Fact]
public async Task SetAndGetString_ShouldStoreAndRetrieveCorrectValue()
```

This test verifies that:
1. A string value can be stored in the cache.
2. The same value can be retrieved from the cache.

Steps:
1. Sets a string value in the cache using a specific key.
2. Retrieves the value from the cache using the same key.
3. Asserts that the retrieved value matches the original value.

### DeleteKey_ShouldRemoveKey

```csharp
[Fact]
public async Task DeleteKey_ShouldRemoveKey()
```

This test verifies that:
1. A value can be stored in the cache.
2. The value can be deleted from the cache.
3. After deletion, the value is no longer retrievable.

Steps:
1. Sets a string value in the cache.
2. Deletes the value using the `RemoveAsync` method.
3. Attempts to retrieve the value and asserts that it's null.

### ExpireKey_ShouldRemoveKeyAfterTTL

```csharp
[Fact]
public async Task ExpireKey_ShouldRemoveKeyAfterTTL()
```

This test verifies that:
1. A value can be stored in the cache with a Time-To-Live (TTL).
2. After the TTL expires, the value is automatically removed from the cache.

Steps:
1. Sets a string value in the cache with a 1-second TTL.
2. Waits for 2 seconds to ensure the TTL has expired.
3. Attempts to retrieve the value and asserts that it's null.

## Cleanup

```csharp
public Task DisposeAsync()
```

This method is called after each test runs. It disposes of the Redis connection. There's a commented-out line that would flush the Redis database, which could be uncommented for a more thorough cleanup between tests.

## Key Components and Services

1. **Serilog**: Used for logging throughout the tests.
2. **Azure Key Vault**: Configured to use an emulator for secure key management.
3. **Redis**: Used as the distributed cache backend.
4. **MemStacheDistributed**: The main service being tested, configured with Redis as the cache provider, SystemTextJson for serialization, and Gzip for compression.
5. **CryptoService**: Used for cryptographic operations.
6. **KeyManagementService**: Used to generate and manage keys.

## Configuration Notes

- The tests use localhost (127.0.0.1:6379) for Redis. This should be adjusted for different environments.
- Azure Key Vault is configured to use an emulator. For production testing, this should be changed to use a real Azure Key Vault instance.
- The MemStache service is configured to use Redis as the distributed cache provider, SystemTextJson for serialization, and Gzip for compression.

## Best Practices Demonstrated

1. **Async/Await**: The test class properly uses asynchronous programming patterns.
2. **Dependency Injection**: Services are configured and resolved using Microsoft's dependency injection container.
3. **Integration Testing**: The tests set up a full environment, testing the interaction between multiple components.
4. **Exception Handling**: The initialization process is wrapped in a try-catch block for better error reporting.

## Potential Improvements

1. Implement the database flushing in `DisposeAsync` to ensure a clean state between test runs.
2. Add more test methods to cover different scenarios and edge cases.
3. Implement parameterized tests to test with various types of data.
4. Add tests for concurrent operations to ensure thread safety.
5. Include tests for the encryption and compression features of MemStache.

## Conclusion

The `MemStacheIntegrationTests` class provides a comprehensive set of integration tests for the MemStache.Distributed library. It demonstrates how to set up a complete test environment, including external services like Redis and Azure Key Vault, and how to perform end-to-end tests of the caching functionality. These tests serve as a valuable tool for ensuring the reliability and correctness of the MemStache library in a realistic, multi-component environment.
