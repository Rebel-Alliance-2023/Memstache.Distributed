using System.Threading;
using System.Threading.Tasks;
using MemStache.Distributed.Secure;

namespace MemStache.Distributed
{
    public interface IMemStacheDistributed
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task<Stash<T>> GetStashAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetStashAsync<T>(Stash<T> stash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
        Task<(Stash<T> Stash, bool Success)> TryGetStashAsync<T>(string key, CancellationToken cancellationToken = default);

        Task<SecureStash<T>> GetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetSecureStashAsync<T>(SecureStash<T> secureStash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
        Task<(SecureStash<T> SecureStash, bool Success)> TryGetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default);
    }
}
