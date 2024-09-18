using System;
using Microsoft.Extensions.DependencyInjection;
using MemStache.Distributed.Factories;
using MemStache.Distributed.Providers;
using MemStache.Distributed.Serialization;
using MemStache.Distributed.Compression;
using MemStache.Distributed.Encryption;
using Microsoft.Extensions.Options;
using MemStache.Distributed.KeyVaultManagement;
using Serilog;
using MemStache.Distributed.Security;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Memstache.Distributed.KeyManagement;
using Emulator = KeyVault.Secrets.Emulator.Rebel.Alliance.KeyVault.Secrets.Emulator;
using Microsoft.Extensions.Logging;

namespace MemStache.Distributed
{
    public static class MemStacheServiceCollectionExtensions
    {
        public static IServiceCollection AddMemStacheDistributed(this IServiceCollection services, Action<MemStacheOptions> setupAction)
        {
            services.Configure(setupAction);

            RegisterFactories(services);
            RegisterCoreServices(services);
            RegisterCacheProviders(services);
            RegisterServiceResolvers(services);

            return services;
        }

        private static void RegisterFactories(IServiceCollection services)
        {
            services.AddSingleton<DistributedCacheProviderFactory>();
            services.AddSingleton<SerializerFactory>();
            services.AddSingleton<CompressorFactory>();
            services.AddSingleton<EncryptorFactory>();
            services.AddSingleton<AzureKeyVaultSecretsFactory>();
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            services.AddSingleton<IMemStacheDistributed, MemStacheDistributed>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<IKeyManagementService, KeyManagementService>();
            services.AddSingleton<Serilog.ILogger>(sp => Log.Logger);
        }

        private static void RegisterCacheProviders(IServiceCollection services)
        {
            services.AddSingleton<RedisDistributedCacheProvider>();
            services.AddSingleton<SystemTextJsonSerializer>();
            services.AddSingleton<GzipCompressor>();
            services.AddSingleton<AesEncryptor>();
        }

        private static void RegisterServiceResolvers(IServiceCollection services)
        {
            services.AddSingleton<Func<IServiceProvider, IDistributedCacheProvider>>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MemStacheOptions>>().Value;
                return options.DistributedCacheProvider switch
                {
                    "Redis" => _ => sp.GetRequiredService<RedisDistributedCacheProvider>(),
                    _ => throw new InvalidOperationException($"Unsupported cache provider: {options.DistributedCacheProvider}")
                };
            });

            services.AddSingleton<Func<IServiceProvider, ISerializer>>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MemStacheOptions>>().Value;
                return options.Serializer switch
                {
                    "SystemTextJson" => _ => sp.GetRequiredService<SystemTextJsonSerializer>(),
                    _ => throw new InvalidOperationException($"Unsupported serializer: {options.Serializer}")
                };
            });

            services.AddSingleton<Func<IServiceProvider, ICompressor>>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MemStacheOptions>>().Value;
                return options.Compressor switch
                {
                    "Gzip" => _ => sp.GetRequiredService<GzipCompressor>(),
                    _ => throw new InvalidOperationException($"Unsupported compressor: {options.Compressor}")
                };
            });
        }

        public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisOptions> setupAction)
        {
            services.Configure(setupAction);
            return services;
        }

        public static IServiceCollection AddAzureKeyVault(this IServiceCollection services, Action<AzureKeyVaultOptions> setupAction, bool useEmulator = false)
        {
            services.Configure(setupAction);

            // Register AzureKeyVaultSecretsWrapper as a singleton instance
            services.AddSingleton<IAzureKeyVaultSecretsWrapper>(sp =>
            {
                IAzureKeyVaultSecrets keyVaultSecrets;

                if (useEmulator)
                {
                    var logger = sp.GetRequiredService<ILogger<Emulator.SecretClient>>();
                    var secretClient = new Emulator.SecretClient(logger);
                    keyVaultSecrets = new AzureKeyVaultEmulator(secretClient);
                }
                else
                {
                    var options = sp.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
                    var secretClient = new SecretClient(new Uri(options.KeyVaultUrl), new DefaultAzureCredential());
                    keyVaultSecrets = new AzureKeyVaultSecrets(secretClient);
                }

                return new AzureKeyVaultSecretsWrapper(sp.GetRequiredService<IOptions<AzureKeyVaultOptions>>(), sp.GetRequiredService<Serilog.ILogger>(), keyVaultSecrets);
            });

            return services;
        }
                  

    }
}