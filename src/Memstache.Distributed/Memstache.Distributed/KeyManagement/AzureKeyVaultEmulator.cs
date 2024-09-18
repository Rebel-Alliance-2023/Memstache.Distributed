using Memstache.Distributed.KeyManagement;
using Rebel.Alliance.KeyVault.Secrets.Emulator;
using System.Collections.Concurrent;
using System.Linq;

public class AzureKeyVaultEmulator : IAzureKeyVaultSecrets
{
    private readonly AzureKeyVaultEmulator _secretClient;

    public AzureKeyVaultEmulator(AzureKeyVaultEmulator secretClient)
    {
        _secretClient = secretClient;
    }


    // Implement the interface methods with mock logic
    public Task DeleteSecretAsync(string name) => Task.CompletedTask;
    public Task<string> GetSecretAsync(string name)
    {
        return _secretClient.GetSecretAsync(name);
    }

    public IAsyncEnumerable<string> ListDeletedSecretsAsync()
    {
        return _secretClient.ListDeletedSecretsAsync();
    }

    public IAsyncEnumerable<ConcurrentDictionary<string, string>> ListPropertiesOfSecretsAsync()
    {
        return _secretClient.ListPropertiesOfSecretsAsync();        
    }

    public Task PurgeDeletedSecretAsync(string name)
    {
        _secretClient.PurgeDeletedSecretAsync(name);
        return Task.CompletedTask;
    }

    public Task RecoverDeletedSecretAsync(string name)
    {
        _secretClient.RecoverDeletedSecretAsync(name);
        return Task.CompletedTask;
    }

    public Task<string> SetSecretAsync(string name, string value)
    {
        return _secretClient.SetSecretAsync(name, value);
    }

    public Task UpdateSecretPropertiesAsync(ConcurrentDictionary<string, string> properties)
    {
        _secretClient.UpdateSecretPropertiesAsync(properties);
        return Task.CompletedTask;
    }
}
