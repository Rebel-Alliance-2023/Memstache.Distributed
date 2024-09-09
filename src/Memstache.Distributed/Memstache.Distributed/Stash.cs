using System;

namespace MemStache.Distributed
{
    /// <summary>
    /// Represents a cache item with metadata.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    public class Stash<T>
    {
        /// <summary>
        /// Gets or sets the unique identifier for the cached item.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value of the cached item.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets the fully qualified name of the type of the stored value.
        /// </summary>
        public string StoredType { get; private set; }

        /// <summary>
        /// Gets or sets the size of the cached item in bytes.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the hash of the cached item for integrity verification.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the cached item.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the caching strategy for this item.
        /// </summary>
        public StashPlan Plan { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Stash{T}"/> class.
        /// </summary>
        /// <param name="key">The unique identifier for the cached item.</param>
        /// <param name="value">The value to be cached.</param>
        /// <param name="plan">The caching strategy to be used.</param>
        public Stash(string key, T value, StashPlan plan = StashPlan.Default)
        {
            Key = key;
            Value = value;
            Plan = plan;
            StoredType = typeof(T).AssemblyQualifiedName;
            ExpirationDate = DateTime.MaxValue;
        }
    }

    /// <summary>
    /// Defines the caching strategy for a <see cref="Stash{T}"/>.
    /// </summary>
    public enum StashPlan
    {
        /// <summary>
        /// Use the default caching strategy.
        /// </summary>
        Default,

        /// <summary>
        /// Only serialize the data without additional processing.
        /// </summary>
        SerializeOnly,

        /// <summary>
        /// Compress the serialized data before caching.
        /// </summary>
        Compress,

        /// <summary>
        /// Encrypt the serialized data before caching.
        /// </summary>
        Encrypt,

        /// <summary>
        /// Compress and then encrypt the serialized data before caching.
        /// </summary>
        CompressAndEncrypt
    }
}
