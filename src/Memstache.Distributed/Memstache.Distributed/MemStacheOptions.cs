using System;

namespace MemStache.Distributed
{
    public class MemStacheOptions
    {
        public string DistributedCacheProvider { get; set; } = "Redis";
        public string KeyManagementProvider { get; set; } = "AzureKeyVault";
        public string Serializer { get; set; } = "SystemTextJson";
        public string Compressor { get; set; }
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
        public EvictionPolicy GlobalEvictionPolicy { get; set; } = EvictionPolicy.LRU;
        public TimeSpan? DefaultAbsoluteExpiration { get; set; }
        public TimeSpan? DefaultSlidingExpiration { get; set; }
    }

}
