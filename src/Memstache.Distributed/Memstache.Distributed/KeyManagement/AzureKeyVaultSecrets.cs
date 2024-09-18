using Azure.Security.KeyVault.Secrets;
using Memstache.Distributed.KeyManagement;
using System.Collections.Concurrent;

public class AzureKeyVaultSecrets : IAzureKeyVaultSecrets
{
    private readonly SecretClient _secretClient;

    public AzureKeyVaultSecrets(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task DeleteSecretAsync(string name)
    {
        _ = await _secretClient.StartDeleteSecretAsync(name);
    }

    public async Task<string> GetSecretAsync(string name)
    {
        Azure.Response<KeyVaultSecret> secret = await _secretClient.GetSecretAsync(name);
        return secret.Value.Value.ToString();
    }

    public async IAsyncEnumerable<string> ListDeletedSecretsAsync()
    {
        await foreach (var secret in _secretClient.GetDeletedSecretsAsync())
        {
            yield return secret.Name.ToString();
        }
    }

    public async IAsyncEnumerable<ConcurrentDictionary<string, string>> ListPropertiesOfSecretsAsync()
    {
        await foreach (var secret in _secretClient.GetPropertiesOfSecretsAsync())
        {
            ConcurrentDictionary<string, string> properties = new();

            _ = properties.TryAdd("Name", secret.Name ?? string.Empty);
            _ = properties.TryAdd("Id", secret.Id?.ToString() ?? string.Empty);
            _ = properties.TryAdd("ContentType", secret.ContentType ?? string.Empty);
            _ = properties.TryAdd("CreatedOn", secret.CreatedOn?.ToString() ?? string.Empty);
            _ = properties.TryAdd("Enabled", secret.Enabled?.ToString() ?? string.Empty);
            _ = properties.TryAdd("ExpiresOn", secret.ExpiresOn?.ToString() ?? string.Empty);
            _ = properties.TryAdd("NotBefore", secret.NotBefore?.ToString() ?? string.Empty);
            _ = properties.TryAdd("RecoveryLevel", secret.RecoveryLevel ?? string.Empty);
            _ = properties.TryAdd("Tags", secret.Tags?.ToString() ?? string.Empty);
            _ = properties.TryAdd("UpdatedOn", secret.UpdatedOn?.ToString() ?? string.Empty);

            yield return properties;
        }
    }

    public async Task PurgeDeletedSecretAsync(string name)
    {
        _ = await _secretClient.PurgeDeletedSecretAsync(name);
    }

    public async Task RecoverDeletedSecretAsync(string name)
    {
        _ = await _secretClient.StartRecoverDeletedSecretAsync(name);
    }

    public async Task<KeyVaultSecret> SetSecretAsync(string name, string value)
    {
        return await _secretClient.SetSecretAsync(name, value);
    }

    public async Task UpdateSecretPropertiesAsync(SecretProperties properties)
    {
        _ = await _secretClient.UpdateSecretPropertiesAsync(properties);
    }

    public Task UpdateSecretPropertiesAsync(ConcurrentDictionary<string, string> properties)
    {
        // Convert properties to SecretProperties
        SecretProperties secretProperties = new(properties["Name"]);

        if (properties.TryGetValue("ContentType", out var contentType))
        {
            secretProperties.ContentType = contentType;
        }
        if (properties.TryGetValue("Enabled", out var enabled))
        {
            secretProperties.Enabled = bool.Parse(enabled);
        }
        if (properties.TryGetValue("NotBefore", out var notBefore))
        {
            secretProperties.NotBefore = DateTimeOffset.Parse(notBefore);
        }
        if (properties.TryGetValue("ExpiresOn", out var expiresOn))
        {
            secretProperties.ExpiresOn = DateTimeOffset.Parse(expiresOn);
        }

        return _secretClient.UpdateSecretPropertiesAsync(secretProperties);
    }

    Task<string> IAzureKeyVaultSecrets.SetSecretAsync(string name, string value)
    {
        throw new NotImplementedException();
    }
}
