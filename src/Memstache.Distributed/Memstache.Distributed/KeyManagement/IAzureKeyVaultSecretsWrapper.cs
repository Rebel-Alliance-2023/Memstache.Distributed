
using System.Collections.Concurrent;

namespace MemStache.Distributed.KeyVaultManagement
{
    public interface IAzureKeyVaultSecretsWrapper
    {
        Task DeleteSecretAsync(string name);
        Task<string> GetSecretAsync(string keyIdentifier, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ListDeletedSecretsAsync();
        IAsyncEnumerable<ConcurrentDictionary<string, string>> ListPropertiesOfSecretsAsync();
        Task PurgeDeletedSecretAsync(string name);
        Task RecoverDeletedSecretAsync(string name);
        Task SetSecretAsync(string keyIdentifier, string keyValue, CancellationToken cancellationToken = default);
        Task UpdateSecretPropertiesAsync(ConcurrentDictionary<string, string> properties);
    }
}