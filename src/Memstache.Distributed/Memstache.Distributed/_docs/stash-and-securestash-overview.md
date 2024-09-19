# Stash and SecureStash: In-Depth 

## Stash<T>: The Foundation of Caching

### Purpose

The `Stash<T>` class serves as the fundamental unit of caching in the MemStache.Distributed library. It encapsulates a cached item along with its metadata, providing a rich and flexible caching mechanism.

### Design

`Stash<T>` is a generic class that can store any type of data. Its key features include:

1. **Key**: A unique identifier for the cached item.
2. **Value**: The actual data being cached.
3. **StoredType**: The type of the stored data, allowing for type-safe retrieval.
4. **Size**: The size of the cached item, useful for cache management.
5. **Hash**: A hash of the data, which can be used for integrity checks.
6. **ExpirationDate**: When the cached item should expire.
7. **Plan**: An enum indicating how the data should be processed (e.g., serialization, compression, encryption).

The `Stash<T>` class is designed to be flexible and extensible. It can be used with different caching strategies as defined by the `StashPlan` enum.

### Usage

`Stash<T>` objects are typically created and managed by the `MemStacheDistributed` class, which provides methods for getting, setting, and managing stashes in the distributed cache.

## SecureStash Preamble:

## Understanding Hierarchical Deterministic (HD) Keys

Before diving into the specifics of SecureStash, it's important to understand the concept of Hierarchical Deterministic (HD) keys, which forms the foundation of our secure key management system.

Hierarchical Deterministic (HD) keys are a system of key generation and management that allows for the creation of a tree-like structure of keys derived from a single master key. This concept was introduced in Bitcoin Improvement Proposal 32 (BIP32) and has since been widely adopted in cryptocurrency wallets and other cryptographic systems.

Key features of HD keys include:

1. **Deterministic Generation**: Given the same master key and derivation path, the same child keys will always be generated.
2. **Hierarchical Structure**: Keys are organized in a tree structure, allowing for logical grouping and organization of keys.
3. **Child Key Independence**: Knowledge of a child key does not reveal information about its siblings or parent keys.
4. **Backup and Recovery**: The entire tree of keys can be recreated from the master key and derivation paths, simplifying backup and recovery processes.

In our implementation, we use a simplified version of this concept. Our `KeyManagementService` generates master keys and derived keys, maintaining the relationship between them. This allows for secure key management and rotation while providing the benefits of hierarchical key structures.


## SecureStash<T>: Enhanced Security for Sensitive Data

### Purpose

`SecureStash<T>` extends the concept of `Stash<T>` by adding encryption capabilities. It's designed for storing sensitive data that requires an extra layer of security.

### Design

`SecureStash<T>` inherits from `IStash<T>` and includes additional features:

1. **EncryptionKeyId**: An identifier for the key used to encrypt the data.
2. **EncryptedData**: The encrypted form of the data.
3. **Encryption and Decryption Methods**: `EncryptAsync()` and `DecryptAsync()` handle the secure transformation of data.
4. **Key Rotation**: The `RotateKeyAsync()` method allows for changing the encryption key without exposing the decrypted data.

`SecureStash<T>` works in conjunction with `IKeyManagementService` and `ICryptoService` to handle key management and cryptographic operations.

### Security Features

1. **Encryption**: Data is encrypted using RSA encryption with OAEP padding and SHA-256 hashing.
2. **Key Management**: Utilizes a hierarchical key structure with master and derived keys.
3. **Separation of Concerns**: Encrypted and decrypted data are never stored simultaneously.
4. **Flexible Crypto Service**: The `ICryptoService` interface allows for easy substitution of cryptographic implementations.

### Usage

`SecureStash<T>` objects can be used similarly to `Stash<T>` objects, but with additional steps for encryption and decryption. The `MemStacheDistributed` class would typically handle these operations transparently to the user.

## Integration in MemStacheDistributed

The `MemStacheDistributed` class provides methods for working with both `Stash<T>` and `SecureStash<T>`:

- `GetStashAsync<T>`, `SetStashAsync<T>`, and `TryGetStashAsync<T>` for regular stashes.
- Similar methods could be implemented for secure stashes, handling encryption and decryption automatically.

The `ProcessAndStoreData` method in `MemStacheDistributed` demonstrates how different `StashPlan` options (like compression and encryption) can be applied to the data before storage.

## Conclusion

The combination of `Stash<T>` and `SecureStash<T>` provides a powerful and flexible caching solution. `Stash<T>` offers a rich metadata-enhanced caching mechanism, while `SecureStash<T>` extends this with robust encryption capabilities. 

This design allows users to choose the appropriate level of security for their data, seamlessly integrating with the distributed caching system. The use of HD key concepts in the key management system further enhances security and manageability, especially for large-scale applications dealing with sensitive data.

By separating the concerns of caching, encryption, and key management, the system remains modular and extensible, allowing for future enhancements and customizations to meet evolving security and performance requirements.
