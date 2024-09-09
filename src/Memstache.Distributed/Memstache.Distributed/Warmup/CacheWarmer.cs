using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MemStache.Distributed.Warmup
{
    public class CacheWarmer : IHostedService
    {
        private readonly IMemStacheDistributed _cache;
        private readonly IEnumerable<ICacheSeeder> _seeders;
        private readonly ILogger _logger;

        public CacheWarmer(IMemStacheDistributed cache, IEnumerable<ICacheSeeder> seeders, ILogger logger)
        {
            _cache = cache;
            _seeders = seeders;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Starting cache warm-up");
            foreach (var seeder in _seeders)
            {
                try
                {
                    await seeder.SeedCacheAsync(_cache, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during cache seeding with {SeederType}", seeder.GetType().Name);
                }
            }
            _logger.Information("Cache warm-up completed");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public interface ICacheSeeder
    {
        Task SeedCacheAsync(IMemStacheDistributed cache, CancellationToken cancellationToken);
    }

    public class ExampleCacheSeeder : ICacheSeeder
    {
        private readonly ILogger _logger;

        public ExampleCacheSeeder(ILogger logger)
        {
            _logger = logger;
        }

        public async Task SeedCacheAsync(IMemStacheDistributed cache, CancellationToken cancellationToken)
        {
            _logger.Information("Seeding example data");
            await cache.SetAsync("example_key", "example_value", cancellationToken: cancellationToken);
            _logger.Information("Example data seeded");
        }
    }

    public static class CacheWarmerExtensions
    {
        public static IServiceCollection AddMemStacheCacheWarmer(this IServiceCollection services)
        {
            services.AddHostedService<CacheWarmer>();
            services.AddTransient<ICacheSeeder, ExampleCacheSeeder>();
            return services;
        }
    }
}
