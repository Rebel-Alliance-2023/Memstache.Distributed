
using System.Collections.Concurrent;

namespace Memstache.Distributed.KeyManagement
{
    public interface IAzureKeyVaultSecrets
    {
        public Task DeleteSecretAsync(string name);
        public Task<string> GetSecretAsync(string name);
        IAsyncEnumerable<string> ListDeletedSecretsAsync();
        public IAsyncEnumerable<ConcurrentDictionary<string, string>> ListPropertiesOfSecretsAsync();
        public Task PurgeDeletedSecretAsync(string name);
        public Task RecoverDeletedSecretAsync(string name);
        public Task<string> SetSecretAsync(string name, string value);
        public Task UpdateSecretPropertiesAsync(ConcurrentDictionary<string, string> properties);
    }
}
