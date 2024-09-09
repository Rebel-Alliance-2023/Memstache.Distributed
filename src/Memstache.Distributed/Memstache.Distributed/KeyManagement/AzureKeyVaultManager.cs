using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Options;
using Serilog;

namespace MemStache.Distributed.KeyManagement
{
    public class AzureKeyVaultManager : IKeyManager
    {
        private readonly KeyClient _keyClient;
        private readonly Serilog.ILogger _logger;
        private readonly AzureKeyVaultOptions _options;

        public AzureKeyVaultManager(IOptions<AzureKeyVaultOptions> options, Serilog.ILogger logger)
        {
            _options = options.Value;
            _logger = logger;
            _keyClient = new KeyClient(new Uri(_options.KeyVaultUrl), new DefaultAzureCredential());
        }

        public async Task<byte[]> GetEncryptionKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _keyClient.GetKeyAsync(keyIdentifier, null, cancellationToken);
                return key.Value.Key.K;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving encryption key {KeyIdentifier} from Azure Key Vault", keyIdentifier);
                throw;
            }
        }

        public async Task RotateKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _keyClient.RotateKeyAsync(keyIdentifier, cancellationToken);
                _logger.Information("Successfully rotated key {KeyIdentifier}", keyIdentifier);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error rotating key {KeyIdentifier} in Azure Key Vault", keyIdentifier);
                throw;
            }
        }
    }

    public class AzureKeyVaultOptions
    {
        public string KeyVaultUrl { get; set; }
    }
}
