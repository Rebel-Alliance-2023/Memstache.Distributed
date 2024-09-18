using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using Memstache.Distributed.KeyManagement;
using System.Collections.Concurrent;

namespace MemStache.Distributed.KeyVaultManagement
{

    public class AzureKeyVaultSecretsWrapper : IAzureKeyVaultSecretsWrapper
    {
        private readonly IAzureKeyVaultSecrets _secretClient;
        private readonly Serilog.ILogger _logger;
        private readonly AzureKeyVaultOptions _options;

        public AzureKeyVaultSecretsWrapper(IOptions<AzureKeyVaultOptions> options, Serilog.ILogger logger, IAzureKeyVaultSecrets secretClient)
        {
            _options = options.Value;
            _logger = logger;
            _secretClient = secretClient;
        }

        public async Task<string> GetSecretAsync(string keyIdentifier, CancellationToken cancellationToken = default)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync(keyIdentifier);
                return secret;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving key {KeyIdentifier} from Azure Key Vault", keyIdentifier);
                throw;
            }
        }

        public async Task SetSecretAsync(string keyIdentifier, string keyValue, CancellationToken cancellationToken = default)
        {
            try
            {
                await _secretClient.SetSecretAsync(keyIdentifier, keyValue);
                _logger.Information("Successfully stored key {KeyIdentifier} in Azure Key Vault", keyIdentifier);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error storing key {KeyIdentifier} in Azure Key Vault", keyIdentifier);
                throw;
            }
        }


        public async Task DeleteSecretAsync(string name)
        {
            try
            {
                await _secretClient.DeleteSecretAsync(name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting secret {SecretName} from Azure Key Vault", name);
                throw;
            }
        }



        public IAsyncEnumerable<string> ListDeletedSecretsAsync()
        {
            try
            {
                return _secretClient.ListDeletedSecretsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error listing deleted secrets from Azure Key Vault");
                throw;
            }
        }

        public IAsyncEnumerable<ConcurrentDictionary<string, string>> ListPropertiesOfSecretsAsync()
        {
            try
            {
                return _secretClient.ListPropertiesOfSecretsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error listing properties of secrets from Azure Key Vault");
                throw;
            }
        }

        public async Task PurgeDeletedSecretAsync(string name)
        {
            try
            {
                await _secretClient.PurgeDeletedSecretAsync(name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error purging deleted secret {SecretName} from Azure Key Vault", name);
                throw;
            }
        }

        public async Task RecoverDeletedSecretAsync(string name)
        {
            try
            {
                await _secretClient.RecoverDeletedSecretAsync(name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error recovering deleted secret {SecretName} from Azure Key Vault", name);
                throw;
            }
        }


        public async Task UpdateSecretPropertiesAsync(ConcurrentDictionary<string, string> properties)
        {
            try
            {
                await _secretClient.UpdateSecretPropertiesAsync(properties);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating properties of secret {SecretName} in Azure Key Vault", properties["Name"]);
                throw;
            }
        }

    }

    public class AzureKeyVaultOptions
    {
        public string KeyVaultUrl { get; set; }
    }
}
