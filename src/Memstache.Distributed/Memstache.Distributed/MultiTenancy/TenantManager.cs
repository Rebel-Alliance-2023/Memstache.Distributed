using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;

namespace MemStache.Distributed.MultiTenancy
{
    public class TenantManager : IMemStacheDistributed
    {
        private readonly IMemStacheDistributed _baseCache;
        private readonly Func<string> _tenantIdProvider;
        private readonly ILogger _logger;

        public TenantManager(IMemStacheDistributed baseCache, Func<string> tenantIdProvider, ILogger logger)
        {
            _baseCache = baseCache;
            _tenantIdProvider = tenantIdProvider;
            _logger = logger;
        }

        private string GetTenantKey(string key)
        {
            string tenantId = _tenantIdProvider();
            return $"{tenantId}:{key}";
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.Information("Getting value for tenant key {TenantKey}", tenantKey);
            return await _baseCache.GetAsync<T>(tenantKey, cancellationToken);
        }

        public async Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.Information("Setting value for tenant key {TenantKey}", tenantKey);
            await _baseCache.SetAsync(tenantKey, value, options, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.Information("Removing value for tenant key {TenantKey}", tenantKey);
            await _baseCache.RemoveAsync(tenantKey, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.Information("Checking existence for tenant key {TenantKey}", tenantKey);
            return await _baseCache.ExistsAsync(tenantKey, cancellationToken);
        }

        public async Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.Information("Trying to get value for tenant key {TenantKey}", tenantKey);
            return await _baseCache.TryGetAsync<T>(tenantKey, cancellationToken);
        }
    }

    public static class TenantManagerExtensions
    {
        public static IServiceCollection AddMemStacheMultiTenancy(this IServiceCollection services, Func<IServiceProvider, Func<string>> tenantIdProviderFactory)
        {
            services.AddScoped<IMemStacheDistributed>(sp =>
            {
                var baseCache = sp.GetRequiredService<MemStacheDistributed>();
                var logger = sp.GetRequiredService<ILogger<TenantManager>>();
                var tenantIdProvider = tenantIdProviderFactory(sp);
                return new TenantManager(baseCache, tenantIdProvider, logger);
            });

            return services;
        }
    }
}
