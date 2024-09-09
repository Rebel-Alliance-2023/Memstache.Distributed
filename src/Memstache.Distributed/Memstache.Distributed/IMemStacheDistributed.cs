using System.Threading;
using System.Threading.Tasks;

namespace MemStache.Distributed
{
    public partial interface IMemStacheDistributed
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
        Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);
    }
}
