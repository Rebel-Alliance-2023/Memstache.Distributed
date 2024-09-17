
namespace MemStache.Distributed.Factories
{
    public interface ICompressorFactory
    {
        ICompressor Create(IServiceProvider serviceProvider);
    }
}