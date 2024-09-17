
namespace MemStache.Distributed.Factories
{
    public interface ISerializerFactory
    {
        ISerializer Create(IServiceProvider serviceProvider);
    }
}