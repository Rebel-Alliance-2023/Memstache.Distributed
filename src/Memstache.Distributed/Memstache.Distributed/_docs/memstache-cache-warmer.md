# CacheWarmer Documentation

This document provides a detailed explanation of the `CacheWarmer.cs` file, which implements a cache warming mechanism for the MemStache.Distributed library.

## Overview

The cache warming feature allows pre-populating the distributed cache with initial data when the application starts. This can improve performance by ensuring that frequently accessed data is already available in the cache from the beginning.

## Components

1. `CacheWarmer` class
2. `ICacheSeeder` interface
3. `ExampleCacheSeeder` class
4. `CacheWarmerExtensions` static class

## CacheWarmer Class

```csharp
public class CacheWarmer : IHostedService
```

The `CacheWarmer` class is responsible for orchestrating the cache warming process.

### Properties

- `private readonly IMemStacheDistributed _cache`: The distributed cache instance.
- `private readonly IEnumerable<ICacheSeeder> _seeders`: A collection of cache seeders.
- `private readonly ILogger _logger`: A logger for recording the warming process.

### Constructor

```csharp
public CacheWarmer(IMemStacheDistributed cache, IEnumerable<ICacheSeeder> seeders, ILogger logger)
```

Initializes a new instance of the `CacheWarmer` class with the necessary dependencies.

### Methods

#### StartAsync

```csharp
public async Task StartAsync(CancellationToken cancellationToken)
```

This method is called when the application starts. It iterates through all registered cache seeders and calls their `SeedCacheAsync` method.

- Logs the start and completion of the cache warm-up process.
- Catches and logs any exceptions that occur during seeding.

#### StopAsync

```csharp
public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
```

This method is called when the application is shutting down. In this implementation, it does nothing and returns a completed task.

## ICacheSeeder Interface

```csharp
public interface ICacheSeeder
```

This interface defines the contract for cache seeders.

### Methods

#### SeedCacheAsync

```csharp
Task SeedCacheAsync(IMemStacheDistributed cache, CancellationToken cancellationToken);
```

This method is implemented by cache seeders to populate the cache with initial data.

## ExampleCacheSeeder Class

```csharp
public class ExampleCacheSeeder : ICacheSeeder
```

This class provides an example implementation of the `ICacheSeeder` interface.

### Properties

- `private readonly ILogger _logger`: A logger for recording the seeding process.

### Constructor

```csharp
public ExampleCacheSeeder(ILogger logger)
```

Initializes a new instance of the `ExampleCacheSeeder` class with a logger.

### Methods

#### SeedCacheAsync

```csharp
public async Task SeedCacheAsync(IMemStacheDistributed cache, CancellationToken cancellationToken)
```

This method demonstrates how to seed the cache with example data:
- Logs the start and completion of the seeding process.
- Sets an example key-value pair in the cache.

## CacheWarmerExtensions Class

```csharp
public static class CacheWarmerExtensions
```

This static class provides extension methods for easy configuration of the cache warming feature.

### Methods

#### AddMemStacheCacheWarmer

```csharp
public static IServiceCollection AddMemStacheCacheWarmer(this IServiceCollection services)
```

This extension method adds the cache warming services to the dependency injection container:
- Registers `CacheWarmer` as a hosted service.
- Registers `ExampleCacheSeeder` as a transient service implementing `ICacheSeeder`.

## Usage

To use the cache warming feature in your application:

1. Call the `AddMemStacheCacheWarmer()` extension method in your `Startup.cs` or `Program.cs` file:

   ```csharp
   services.AddMemStacheCacheWarmer();
   ```

2. Implement your own cache seeders by creating classes that implement the `ICacheSeeder` interface.

3. Register your custom cache seeders in the dependency injection container:

   ```csharp
   services.AddTransient<ICacheSeeder, YourCustomSeeder>();
   ```

The `CacheWarmer` will automatically run when your application starts, calling all registered seeders to populate the cache.

## Best Practices

1. Keep seeding operations lightweight to avoid delaying application startup.
2. Use cancellation tokens to make seeding operations cancellable.
3. Implement proper error handling in your seeders to prevent one faulty seeder from stopping the entire warming process.
4. Use logging to track the progress and success of the cache warming process.

## Conclusion

The cache warming feature provides a flexible and extensible way to pre-populate your distributed cache. By implementing custom seeders, you can ensure that your application has the necessary data cached from the start, potentially improving initial performance and user experience.
