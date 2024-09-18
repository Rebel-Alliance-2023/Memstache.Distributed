using KeyVault.Secrets.Emulator.Rebel.Alliance.KeyVault.Secrets.Emulator;
using Memstache.Distributed.KeyManagement;
using Rebel.Alliance.KeyVault.Secrets.Emulator;
using System.Collections.Concurrent;
using System.Linq;

namespace MemStache.Distributed.KeyVaultManagement;
public class AzureKeyVaultEmulator : IAzureKeyVaultSecrets
{
    private readonly SecretClient _secretClient;

    public AzureKeyVaultEmulator(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }


    // Implement the interface methods with mock logic
    public Task DeleteSecretAsync(string name) => Task.CompletedTask;
    public async Task<string> GetSecretAsync(string name)
    {
        KeyVaultSecret secret = await _secretClient.GetSecretAsync(name);
        return secret.Value;
    }

    public IAsyncEnumerable<string> ListDeletedSecretsAsync()
    {
        IAsyncEnumerable<KeyVaultSecret> secrets = _secretClient.ListDeletedSecretsAsync();
        return secrets.Select(x => x.Value);
    }

    public IAsyncEnumerable<ConcurrentDictionary<string, string>> ListPropertiesOfSecretsAsync()
    {
        IAsyncEnumerable<SecretProperties> secretProperties = _secretClient.ListPropertiesOfSecretsAsync();
        return secretProperties.Select(x => new ConcurrentDictionary<string, string>((IEnumerable<KeyValuePair<string, string>>)x));
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

    public async Task<string> SetSecretAsync(string name, string value)
    {
        KeyVaultSecret secret = await _secretClient.SetSecretAsync(name, value);
        return secret.Value.ToString();
    }

    public async Task UpdateSecretPropertiesAsync(ConcurrentDictionary<string, string> properties)
    {
        // Create a new instance of SecretProperties
        var secretProperties = new SecretProperties();

        // Map the dictionary entries to the SecretProperties object
        foreach (var property in properties)
        {
            // Assuming the dictionary keys match the property names of SecretProperties
            // Use reflection to set the properties dynamically
            var propertyInfo = typeof(SecretProperties).GetProperty(property.Key);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(secretProperties, property.Value);
            }
        }

        // Assuming you have a method to update the secret properties in the SecretClient
        await _secretClient.UpdateSecretPropertiesAsync(secretProperties);
    }
}
