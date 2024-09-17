
namespace MemStache.Distributed.Factories
{
    public interface IDistributedCacheProviderFactory
    {
        IDistributedCacheProvider Create(IServiceProvider serviceProvider);
    }
}