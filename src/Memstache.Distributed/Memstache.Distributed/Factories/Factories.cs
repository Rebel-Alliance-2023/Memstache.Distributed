using System;
using Memstache.Distributed.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace MemStache.Distributed.Factories
{
    public class DistributedCacheProviderFactory : IDistributedCacheProviderFactory
    {
        private readonly Func<IServiceProvider, IDistributedCacheProvider> _factory;

        public DistributedCacheProviderFactory(Func<IServiceProvider, IDistributedCacheProvider> factory)
        {
            _factory = factory;
        }

        public IDistributedCacheProvider Create(IServiceProvider serviceProvider)
        {
            return _factory(serviceProvider);
        }
    }

    public class SerializerFactory : ISerializerFactory
    {
        private readonly Func<IServiceProvider, ISerializer> _factory;

        public SerializerFactory(Func<IServiceProvider, ISerializer> factory)
        {
            _factory = factory;
        }

        public ISerializer Create(IServiceProvider serviceProvider)
        {
            return _factory(serviceProvider);
        }
    }

    public class CompressorFactory : ICompressorFactory
    {
        private readonly Func<IServiceProvider, ICompressor> _factory;

        public CompressorFactory(Func<IServiceProvider, ICompressor> factory)
        {
            _factory = factory;
        }

        public ICompressor Create(IServiceProvider serviceProvider)
        {
            return _factory(serviceProvider);
        }
    }

    public class EncryptorFactory
    {
        private readonly Func<IServiceProvider, IEncryptor> _factory;

        public EncryptorFactory(Func<IServiceProvider, IEncryptor> factory)
        {
            _factory = factory;
        }

        public IEncryptor Create(IServiceProvider serviceProvider)
        {
            return _factory(serviceProvider);
        }
    }

    public class AzureKeyVaultSecretsFactory
    {
        private readonly Func<IServiceProvider, IAzureKeyVaultSecrets> _factory;

        public AzureKeyVaultSecretsFactory(Func<IServiceProvider, IAzureKeyVaultSecrets> factory)
        {
            _factory = factory;
        }

        public IAzureKeyVaultSecrets Create(IServiceProvider serviceProvider)
        {
            return _factory(serviceProvider);
        }
    }
}
