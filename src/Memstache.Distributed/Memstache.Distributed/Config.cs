using System;

namespace MemStache.Distributed
{
    public class MemStacheOptions
    {
        public string DistributedCacheProvider { get; set; } = "Redis";
        public string KeyManagementProvider { get; set; } = "AzureKeyVault";
        public string Serializer { get; set; } = "SystemTextJson";
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
        public EvictionPolicy GlobalEvictionPolicy { get; set; } = EvictionPolicy.LRU;
        public TimeSpan? DefaultAbsoluteExpiration { get; set; }
        public TimeSpan? DefaultSlidingExpiration { get; set; }
    }

    public class MemStacheEntryOptions
    {
        public TimeSpan? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public EvictionPolicy EvictionPolicy { get; set; }
        public bool Compress { get; set; }
        public bool Encrypt { get; set; }
    }
}
