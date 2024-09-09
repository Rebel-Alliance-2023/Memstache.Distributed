using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using MemStache.Distributed.Factories;
using MemStache.Distributed.EvictionPolicies;

namespace MemStache.Distributed
{
    public class MemStacheDistributed : IMemStacheDistributed
    {
        private readonly IDistributedCacheProvider _cacheProvider;
        private readonly ISerializer _serializer;
        private readonly ICompressor _compressor;
        private readonly IEncryptor _encryptor;
        private readonly IKeyManager _keyManager;
        private readonly IEvictionPolicy _evictionPolicy;
        private readonly MemStacheOptions _options;
        private readonly ILogger _logger;

        public MemStacheDistributed(
            DistributedCacheProviderFactory cacheProviderFactory,
            SerializerFactory serializerFactory,
            CompressorFactory compressorFactory,
            EncryptorFactory encryptorFactory,
            KeyManagerFactory keyManagerFactory,
            IOptions<MemStacheOptions> options,
            ILogger logger)
        {
            _options = options.Value;
            _logger = logger;

            _cacheProvider = cacheProviderFactory.Create(_options.DistributedCacheProvider);
            _serializer = serializerFactory.Create(_options.Serializer);
            _compressor = compressorFactory.Create("gzip");
            _encryptor = encryptorFactory.Create("aes");
            _keyManager = keyManagerFactory.Create(_options.KeyManagementProvider);
            _evictionPolicy = CreateEvictionPolicy(_options.GlobalEvictionPolicy);
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await TryGetAsync<T>(key, cancellationToken);
                return result.Success ? result.Value : default;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving value for key {Key}", key);
                throw;
            }
        }

        public async Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _cacheProvider.GetAsync(key, cancellationToken);
                if (data == null)
                {
                    return (default, false);
                }

                _evictionPolicy.RecordAccess(key);

                if (_options.EnableEncryption)
                {
                    var encryptionKey = await _keyManager.GetEncryptionKeyAsync(key, cancellationToken);
                    data = _encryptor.Decrypt(data, encryptionKey);
                }

                if (_options.EnableCompression)
                {
                    data = _compressor.Decompress(data);
                }

                var value = _serializer.Deserialize<T>(data);
                return (value, true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error retrieving value for key {Key}", key);
                return (default, false);
            }
        }

        public async Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            try
            {
                options ??= new MemStacheEntryOptions();
                var data = _serializer.Serialize(value);

                if (_options.EnableCompression || options.Compress)
                {
                    data = _compressor.Compress(data);
                }

                if (_options.EnableEncryption || options.Encrypt)
                {
                    var encryptionKey = await _keyManager.GetEncryptionKeyAsync(key, cancellationToken);
                    data = _encryptor.Encrypt(data, encryptionKey);
                }

                await _cacheProvider.SetAsync(key, data, options, cancellationToken);
                _evictionPolicy.RecordAccess(key);

                _logger.Information("Successfully set value for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error setting value for key {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cacheProvider.RemoveAsync(key, cancellationToken);
                _logger.Information("Successfully removed key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error removing key {Key}", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _cacheProvider.ExistsAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking existence of key {Key}", key);
                throw;
            }
        }

        private IEvictionPolicy CreateEvictionPolicy(EvictionPolicy policy)
        {
            return policy switch
            {
                EvictionPolicy.LRU => new LruEvictionPolicy(_logger),
                EvictionPolicy.LFU => new LfuEvictionPolicy(_logger),
                EvictionPolicy.TimeBased => new TimeBasedEvictionPolicy(_logger),
                _ => new LruEvictionPolicy(_logger), // Default to LRU
            };
        }
    }
}
