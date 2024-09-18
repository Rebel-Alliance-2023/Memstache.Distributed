using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using MemStache.Distributed.Factories;
using MemStache.Distributed.EvictionPolicies;
using MemStache.Distributed.Secure;
using MemStache.Distributed.Security;

namespace MemStache.Distributed
{
    public partial class MemStacheDistributed : IMemStacheDistributed
    {
        private readonly IDistributedCacheProvider _cacheProvider;
        private readonly ISerializer _serializer;
        private readonly ICompressor _compressor;
        private readonly ICryptoService _cryptoService;
        private readonly IKeyManagementService _keyManagementService;
        private readonly IEvictionPolicy _evictionPolicy;
        private readonly MemStacheOptions _options;
        private readonly ILogger _logger;

        public MemStacheDistributed(
            DistributedCacheProviderFactory cacheProviderFactory,
            SerializerFactory serializerFactory,
            CompressorFactory compressorFactory,
            ICryptoService cryptoService,
            IKeyManagementService keyManagementService,
            IOptions<MemStacheOptions> options,
            ILogger logger,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _logger = logger;

            _cacheProvider = cacheProviderFactory.Create(serviceProvider);
            _serializer = serializerFactory.Create(serviceProvider);
            _compressor = compressorFactory.Create(serviceProvider);
            _cryptoService = cryptoService;
            _keyManagementService = keyManagementService;
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
                    var encryptionKey = await _keyManagementService.GetDerivedKeyAsync(key);
                    data = _cryptoService.DecryptData(encryptionKey.PrivateKey, data);
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
                    var encryptionKey = await _keyManagementService.GenerateDerivedKeyAsync();
                    data = _cryptoService.EncryptData(encryptionKey.PublicKey, data);
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

        public async Task<Stash<T>> GetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var data = await _cacheProvider.GetAsync(key, cancellationToken);
            if (data == null)
                return null;

            var stash = _serializer.Deserialize<Stash<T>>(data);
            _evictionPolicy.RecordAccess(key);

            return stash;
        }

        public async Task SetStashAsync<T>(Stash<T> stash, MemStacheEntryOptions options = null, CancellationToken cancellationToken = default)
        {
            options ??= new MemStacheEntryOptions();
            var data = _serializer.Serialize(stash);

            await ProcessAndStoreData(stash.Key, data, stash.Plan, options, cancellationToken);
            _evictionPolicy.RecordAccess(stash.Key);
        }

        public async Task<(Stash<T> Stash, bool Success)> TryGetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var stash = await GetStashAsync<T>(key, cancellationToken);
                return (stash, stash != null);
            }
            catch
            {
                return (null, false);
            }
        }

        public async Task<SecureStash<T>> GetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var stash = await GetStashAsync<SecureStash<T>>(key, cancellationToken);
            if (stash == null)
                return null;

            var secureStash = stash.Value;
            await secureStash.DecryptAsync();
            return secureStash;
        }

        public async Task SetSecureStashAsync<T>(SecureStash<T> secureStash, MemStacheEntryOptions options = null, CancellationToken cancellationToken = default)
        {
            await secureStash.EncryptAsync();
            var stash = new Stash<SecureStash<T>>(secureStash.Key, secureStash, StashPlan.SerializeOnly);
            await SetStashAsync(stash, options, cancellationToken);
        }

        public async Task<(SecureStash<T> SecureStash, bool Success)> TryGetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var secureStash = await GetSecureStashAsync<T>(key, cancellationToken);
                return (secureStash, secureStash != null);
            }
            catch
            {
                return (null, false);
            }
        }

        private async Task ProcessAndStoreData(string key, byte[] data, StashPlan plan, MemStacheEntryOptions options, CancellationToken cancellationToken)
        {
            switch (plan)
            {
                case StashPlan.Compress:
                    data = _compressor.Compress(data);
                    break;
                case StashPlan.Encrypt:
                    var encryptionKey = await _keyManagementService.GenerateDerivedKeyAsync();
                    data = _cryptoService.EncryptData(encryptionKey.PublicKey, data);
                    break;
                case StashPlan.CompressAndEncrypt:
                    data = _compressor.Compress(data);
                    encryptionKey = await _keyManagementService.GenerateDerivedKeyAsync();
                    data = _cryptoService.EncryptData(encryptionKey.PublicKey, data);
                    break;
                case StashPlan.SerializeOnly:
                case StashPlan.Default:
                    // Data is already serialized, do nothing
                    break;
            }

            await _cacheProvider.SetAsync(key, data, options, cancellationToken);
        }

    }
}
