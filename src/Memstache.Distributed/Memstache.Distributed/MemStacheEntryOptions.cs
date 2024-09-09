namespace MemStache.Distributed
{
    public class MemStacheEntryOptions
    {
        /// <summary>
        /// Gets or sets the absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration time for the cache entry.
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compress the cache entry.
        /// </summary>
        public bool Compress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to encrypt the cache entry.
        /// </summary>
        public bool Encrypt { get; set; }
    }
}
