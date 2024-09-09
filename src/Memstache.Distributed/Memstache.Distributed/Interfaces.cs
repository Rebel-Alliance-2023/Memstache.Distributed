using System;
using System.Threading;
using System.Threading.Tasks;

namespace MemStache.Distributed
{
    public interface IDistributedCacheProvider
    {
        Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(string key, byte[] value, MemStacheEntryOptions options, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }

    public interface ISerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] data);
    }

    public interface ICompressor
    {
        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] compressedData);
    }

    public interface IEncryptor
    {
        byte[] Encrypt(byte[] data, byte[] key);
        byte[] Decrypt(byte[] encryptedData, byte[] key);
    }

    public interface IKeyManager
    {
        Task<byte[]> GetEncryptionKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default);
        Task RotateKeyAsync(string keyIdentifier, CancellationToken cancellationToken = default);
    }

    public enum EvictionPolicy
    {
        Default,
        LRU,
        LFU,
        TimeBased,
        Custom
    }
}
