using System;
using Microsoft.Extensions.DependencyInjection;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Encryption;
using MemStache.Distributed.KeyManagement;

namespace MemStache.Distributed.Factories
{
    public class DistributedCacheProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DistributedCacheProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDistributedCacheProvider Create(string providerName)
        {
            return providerName.ToLowerInvariant() switch
            {
                "redis" => _serviceProvider.GetRequiredService<RedisDistributedCacheProvider>(),
                _ => throw new ArgumentException($"Unsupported cache provider: {providerName}")
            };
        }
    }

    public class SerializerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SerializerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ISerializer Create(string serializerName)
        {
            return serializerName.ToLowerInvariant() switch
            {
                "systemtextjson" => _serviceProvider.GetRequiredService<SystemTextJsonSerializer>(),
                _ => throw new ArgumentException($"Unsupported serializer: {serializerName}")
            };
        }
    }

    public class CompressorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CompressorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICompressor Create(string compressorName)
        {
            return compressorName.ToLowerInvariant() switch
            {
                "gzip" => _serviceProvider.GetRequiredService<GzipCompressor>(),
                _ => throw new ArgumentException($"Unsupported compressor: {compressorName}")
            };
        }
    }

    public class EncryptorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public EncryptorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEncryptor Create(string encryptorName)
        {
            return encryptorName.ToLowerInvariant() switch
            {
                "aes" => _serviceProvider.GetRequiredService<AesEncryptor>(),
                _ => throw new ArgumentException($"Unsupported encryptor: {encryptorName}")
            };
        }
    }

    public class KeyManagerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public KeyManagerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IKeyManager Create(string keyManagerName)
        {
            return keyManagerName.ToLowerInvariant() switch
            {
                "azurekeyvault" => _serviceProvider.GetRequiredService<AzureKeyVaultManager>(),
                _ => throw new ArgumentException($"Unsupported key manager: {keyManagerName}")
            };
        }
    }
}
