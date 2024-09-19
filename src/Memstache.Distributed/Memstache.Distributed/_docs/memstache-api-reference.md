# MemStache.Distributed API Reference

This API Reference provides details on the key interfaces and classes in MemStache.Distributed. It serves as a quick reference for developers using the library.

## IMemStacheDistributed Interface

The `IMemStacheDistributed` interface is the primary point of interaction for users of MemStache.Distributed. It defines the core operations for distributed caching.

```csharp
public interface IMemStacheDistributed
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<(T? Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task<Stash<T>> GetStashAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetStashAsync<T>(Stash<T> stash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<(Stash<T> Stash, bool Success)> TryGetStashAsync<T>(string key, CancellationToken cancellationToken = default);

    Task<SecureStash<T>> GetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetSecureStashAsync<T>(SecureStash<T> secureStash, MemStacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<(SecureStash<T> SecureStash, bool Success)> TryGetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default);
}
```

### Method Descriptions

- `GetAsync<T>`: Retrieves the value with the given key.
- `SetAsync<T>`: Sets the value with the given key.
- `RemoveAsync`: Removes the value with the given key.
- `ExistsAsync`: Checks if a value exists for the given key.
- `TryGetAsync<T>`: Attempts to get the value, returning a tuple with the value and a success flag.
- `GetStashAsync<T>`: Retrieves a Stash object for the given key.
- `SetStashAsync<T>`: Sets a Stash object for the given key.
- `TryGetStashAsync<T>`: Attempts to get a Stash object, returning a tuple with the Stash and a success flag.
- `GetSecureStashAsync<T>`: Retrieves a SecureStash object for the given key.
- `SetSecureStashAsync<T>`: Sets a SecureStash object for the given key.
- `TryGetSecureStashAsync<T>`: Attempts to get a SecureStash object, returning a tuple with the SecureStash and a success flag.

## Key Classes and Interfaces

### MemStacheEntryOptions

Defines options for cache entries.

```csharp
public class MemStacheEntryOptions
{
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public bool Compress { get; set; }
    public bool Encrypt { get; set; }
}
```

### Stash<T>

Represents a cache item with metadata.

```csharp
public class Stash<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public string StoredType { get; }
    public int Size { get; set; }
    public string Hash { get; set; }
    public DateTime ExpirationDate { get; set; }
    public StashPlan Plan { get; set; }

    public Stash(string key, T value, StashPlan plan = StashPlan.Default);
}
```

### SecureStash<T>

Extends Stash<T> with encryption capabilities.

```csharp
public class SecureStash<T> : IStash<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public string StoredType { get; }
    public int Size { get; set; }
    public string Hash { get; set; }
    public DateTime ExpirationDate { get; set; }
    public StashPlan Plan { get; set; }
    public string EncryptionKeyId { get; set; }
    public byte[] EncryptedData { get; set; }

    public SecureStash(IKeyManagementService keyManagementService, ICryptoService cryptoService);

    public Task EncryptAsync();
    public Task DecryptAsync();
    public Task RotateKeyAsync();
}
```

### IDistributedCacheProvider

Defines the interface for distributed cache providers.

```csharp
public interface IDistributedCacheProvider
{
    Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, byte[] value, MemStacheEntryOptions options, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
```

### ISerializer

Defines the interface for serializers.

```csharp
public interface ISerializer
{
    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[] data);
}
```

### ICompressor

Defines the interface for compressors.

```csharp
public interface ICompressor
{
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] compressedData);
}
```

### IEncryptor

Defines the interface for encryptors.

```csharp
public interface IEncryptor
{
    byte[] Encrypt(byte[] data, byte[] key);
    byte[] Decrypt(byte[] encryptedData, byte[] key);
}
```

### IKeyManagementService

Defines the interface for key management services.

```csharp
public interface IKeyManagementService
{
    Task<MasterKey> GenerateMasterKeyAsync();
    Task<DerivedKey> GenerateDerivedKeyAsync(string masterKeyId = null);
    Task<MasterKey> GetMasterKeyAsync(string keyId = null);
    Task<DerivedKey> GetDerivedKeyAsync(string keyId = null);
    Task<MasterKey> RotateMasterKeyAsync(string masterKeyId = null);
}
```

### ICryptoService

Defines the interface for cryptographic operations.

```csharp
public interface ICryptoService
{
    string GenerateMnemonic();
    (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic);
    byte[] EncryptData(byte[] publicKey, byte[] data);
    byte[] DecryptData(byte[] privateKey, byte[] data);
    byte[] SignData(byte[] privateKey, byte[] data);
    bool VerifyData(byte[] publicKey, byte[] data, byte[] signature);
}
```

### IEvictionPolicy

Defines the interface for cache eviction policies.

```csharp
public interface IEvictionPolicy
{
    void RecordAccess(string key);
    string SelectVictim();
}
```

This API Reference provides an overview of the main interfaces and classes in MemStache.Distributed. Developers can use this as a quick reference when working with the library. For more detailed information on usage and implementation, refer to the full documentation and usage guide.

