using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using MemStache.Distributed;
using MemStache.Distributed.Secure;

namespace MemStache.Distributed.Adapters
{
    public class MemStacheDistributedCacheAdapter : IMemStacheDistributed, IDistributedCache
    {
        private readonly IMemStacheDistributed _memStacheDistributed;

        public MemStacheDistributedCacheAdapter(IMemStacheDistributed memStacheDistributed)
        {
            _memStacheDistributed = memStacheDistributed;
        }

        #region IDistributedCache Implementation

        public byte[]? Get(string key)
        {
            return GetAsync(key).GetAwaiter().GetResult();
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            var result = await _memStacheDistributed.TryGetAsync<byte[]>(key, token);
            return result.Success ? result.Value : null;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, options).GetAwaiter().GetResult();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            var memStacheOptions = new MemStacheEntryOptions
            {
                AbsoluteExpiration = options.AbsoluteExpiration,
                SlidingExpiration = options.SlidingExpiration
            };

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                memStacheOptions.AbsoluteExpiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            return _memStacheDistributed.SetAsync(key, value, memStacheOptions, token);
        }

        public void Refresh(string key)
        {
            RefreshAsync(key).GetAwaiter().GetResult();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            var result = await _memStacheDistributed.TryGetAsync<byte[]>(key, token);
            if (result.Success)
            {
                await _memStacheDistributed.SetAsync(key, result.Value, cancellationToken: token);
            }
        }

        public void Remove(string key)
        {
            RemoveAsync(key).GetAwaiter().GetResult();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return _memStacheDistributed.RemoveAsync(key, token);
        }

        #endregion

        #region IMemStacheDistributed Implementation

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.GetAsync<T>(key, cancellationToken);
        }

        public Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.SetAsync(key, value, options, cancellationToken);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.ExistsAsync(key, cancellationToken);
        }

        public Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.TryGetAsync<T>(key, cancellationToken);
        }

        public Task<Stash<T>> GetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.GetStashAsync<T>(key, cancellationToken);
        }

        public Task SetStashAsync<T>(Stash<T> stash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.SetStashAsync(stash, options, cancellationToken);
        }

        public Task<(Stash<T> Stash, bool Success)> TryGetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.TryGetStashAsync<T>(key, cancellationToken);
        }

        public Task<SecureStash<T>> GetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.GetSecureStashAsync<T>(key, cancellationToken);
        }

        public Task SetSecureStashAsync<T>(SecureStash<T> secureStash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.SetSecureStashAsync(secureStash, options, cancellationToken);
        }

        public Task<(SecureStash<T> SecureStash, bool Success)> TryGetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _memStacheDistributed.TryGetSecureStashAsync<T>(key, cancellationToken);
        }

        #endregion
    }
}
