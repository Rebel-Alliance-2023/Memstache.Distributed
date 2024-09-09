using System;
using Microsoft.Extensions.DependencyInjection;
using MemStache.Distributed.Factories;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Encryption;
using MemStache.Distributed.KeyManagement;

namespace MemStache.Distributed
{
    public static class MemStacheServiceCollectionExtensions
    {
        public static IServiceCollection AddMemStacheDistributed(this IServiceCollection services, Action<MemStacheOptions> setupAction)
        {
            services.Configure(setupAction);

            services.AddSingleton<DistributedCacheProviderFactory>();
            services.AddSingleton<SerializerFactory>();
            services.AddSingleton<CompressorFactory>();
            services.AddSingleton<EncryptorFactory>();
            services.AddSingleton<KeyManagerFactory>();

            services.AddSingleton<IMemStacheDistributed, MemStacheDistributed>();

            services.AddSingleton<RedisDistributedCacheProvider>();
            services.AddSingleton<SystemTextJsonSerializer>();
            services.AddSingleton<GzipCompressor>();
            services.AddSingleton<AesEncryptor>();
            services.AddSingleton<AzureKeyVaultManager>();

            return services;
        }

        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisOptions> setupAction)
        {
            services.Configure(setupAction);
            return services;
        }

        public static IServiceCollection AddAzureKeyVault(this IServiceCollection services, Action<AzureKeyVaultOptions> setupAction)
        {
            services.Configure(setupAction);
            return services;
        }
    }
}
