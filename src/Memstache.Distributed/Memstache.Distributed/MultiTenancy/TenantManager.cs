using System;
using System.Threading;
using System.Threading.Tasks;
using MemStache.Distributed.Secure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemStache.Distributed.MultiTenancy
{
    public class TenantManager : IMemStacheDistributed
    {
        private readonly IMemStacheDistributed _baseCache;
        private readonly Func<string> _tenantIdProvider;
        private readonly ILogger<TenantManager> _logger;

        public TenantManager(IMemStacheDistributed baseCache, Func<string> tenantIdProvider, ILogger<TenantManager> logger)
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
            _logger.LogInformation("Getting value for tenant key {TenantKey}", tenantKey);
            return await _baseCache.GetAsync<T>(tenantKey, cancellationToken);
        }

        public async Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Setting value for tenant key {TenantKey}", tenantKey);
            await _baseCache.SetAsync(tenantKey, value, options, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Removing value for tenant key {TenantKey}", tenantKey);
            await _baseCache.RemoveAsync(tenantKey, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Checking existence for tenant key {TenantKey}", tenantKey);
            return await _baseCache.ExistsAsync(tenantKey, cancellationToken);
        }

        public async Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Trying to get value for tenant key {TenantKey}", tenantKey);
            return await _baseCache.TryGetAsync<T>(tenantKey, cancellationToken);
        }

        public async Task<Stash<T>> GetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Getting stash for tenant key {TenantKey}", tenantKey);
            return await _baseCache.GetStashAsync<T>(tenantKey, cancellationToken);
        }

        public async Task SetStashAsync<T>(Stash<T> stash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(stash.Key);
            _logger.LogInformation("Setting stash for tenant key {TenantKey}", tenantKey);
            stash.Key = tenantKey; // Update the key to include tenant information
            await _baseCache.SetStashAsync(stash, options, cancellationToken);
        }

        public async Task<(Stash<T> Stash, bool Success)> TryGetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Trying to get stash for tenant key {TenantKey}", tenantKey);
            return await _baseCache.TryGetStashAsync<T>(tenantKey, cancellationToken);
        }

        public async Task<SecureStash<T>> GetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Getting secure stash for tenant key {TenantKey}", tenantKey);
            return await _baseCache.GetSecureStashAsync<T>(tenantKey, cancellationToken);
        }

        public async Task SetSecureStashAsync<T>(SecureStash<T> secureStash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(secureStash.Key);
            _logger.LogInformation("Setting secure stash for tenant key {TenantKey}", tenantKey);
            secureStash.Key = tenantKey; // Update the key to include tenant information
            await _baseCache.SetSecureStashAsync(secureStash, options, cancellationToken);
        }

        public async Task<(SecureStash<T> SecureStash, bool Success)> TryGetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string tenantKey = GetTenantKey(key);
            _logger.LogInformation("Trying to get secure stash for tenant key {TenantKey}", tenantKey);
            return await _baseCache.TryGetSecureStashAsync<T>(tenantKey, cancellationToken);
        }
    }

    public static class TenantManagerExtensions
    {
        public static IServiceCollection AddMemStacheMultiTenancy(this IServiceCollection services, Func<IServiceProvider, Func<string>> tenantIdProviderFactory)
        {
            services.AddScoped<IMemStacheDistributed>(sp =>
            {
                var baseCache = sp.GetRequiredService<IMemStacheDistributed>();
                var logger = sp.GetRequiredService<ILogger<TenantManager>>();
                var tenantIdProvider = tenantIdProviderFactory(sp);
                return new TenantManager(baseCache, tenantIdProvider, logger);
            });

            return services;
        }
    }
}
