using System;
using Microsoft.Extensions.DependencyInjection;

namespace MemStache.Distributed.Factories
{
    public class DistributedCacheProviderFactory
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

    public class SerializerFactory
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

    public class CompressorFactory
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

    public class KeyManagerFactory
    {
        private readonly Func<IServiceProvider, IKeyManager> _factory;

        public KeyManagerFactory(Func<IServiceProvider, IKeyManager> factory)
        {
            _factory = factory;
        }

        public IKeyManager Create(IServiceProvider serviceProvider)
        {
            return _factory(serviceProvider);
        }
    }
}
