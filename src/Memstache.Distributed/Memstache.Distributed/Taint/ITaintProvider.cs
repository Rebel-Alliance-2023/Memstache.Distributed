using System;
using System.Threading.Tasks;

namespace MemStache.Distributed.TaintStash
{
    public interface ITaintProvider
    {
        /// <summary>
        /// Generates a new taint signature for the given key identifier.
        /// </summary>
        /// <param name="keyId">The identifier of the key to generate the taint signature for.</param>
        /// <returns>A new TaintSignature.</returns>
        Task<TaintSignature> GenerateTaintSignatureAsync(string keyId);

        /// <summary>
        /// Verifies a taint signature against the current environment and key.
        /// </summary>
        /// <param name="keyId">The identifier of the key used to verify the signature.</param>
        /// <param name="taintSignature">The taint signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> VerifyTaintSignatureAsync(string keyId, TaintSignature taintSignature);

        /// <summary>
        /// Combines two taint signatures into a new one.
        /// </summary>
        /// <param name="signature1">The first taint signature.</param>
        /// <param name="signature2">The second taint signature.</param>
        /// <param name="keyId">The identifier of the key to use for the new signature.</param>
        /// <returns>A new TaintSignature that combines the attributes of both input signatures.</returns>
        Task<TaintSignature> CombineTaintSignaturesAsync(TaintSignature signature1, TaintSignature signature2, string keyId);

        /// <summary>
        /// Encrypts data with a taint signature.
        /// </summary>
        /// <param name="keyId">The identifier of the key to use for encryption.</param>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="taintSignature">The taint signature to include with the encrypted data.</param>
        /// <returns>The encrypted data with the embedded taint signature.</returns>
        Task<byte[]> EncryptWithTaintAsync(string keyId, byte[] data, TaintSignature taintSignature);

        /// <summary>
        /// Decrypts data and extracts the taint signature.
        /// </summary>
        /// <param name="keyId">The identifier of the key to use for decryption.</param>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <returns>A tuple containing the decrypted data and the extracted taint signature.</returns>
        Task<(byte[] DecryptedData, TaintSignature ExtractedTaintSignature)> DecryptWithTaintAsync(string keyId, byte[] encryptedData);

        /// <summary>
        /// Generates a compilation target profile for the current environment.
        /// </summary>
        /// <returns>A CompilationTargetProfile representing the current environment.</returns>
        Task<CompilationTargetProfile> GenerateCompilationTargetProfileAsync();

        /// <summary>
        /// Verifies if the current environment matches the given compilation target profile.
        /// </summary>
        /// <param name="profile">The CompilationTargetProfile to verify against.</param>
        /// <returns>True if the current environment matches the profile, false otherwise.</returns>
        Task<bool> VerifyCompilationTargetProfileAsync(CompilationTargetProfile profile);

        /// <summary>
        /// Updates the taint signature for a given key.
        /// </summary>
        /// <param name="keyId">The identifier of the key to update.</param>
        /// <param name="newTaintSignature">The new taint signature to associate with the key.</param>
        Task UpdateKeyTaintAsync(string keyId, TaintSignature newTaintSignature);

        /// <summary>
        /// Retrieves the current taint signature for a given key.
        /// </summary>
        /// <param name="keyId">The identifier of the key to retrieve the taint signature for.</param>
        /// <returns>The current TaintSignature associated with the key.</returns>
        Task<TaintSignature> GetKeyTaintAsync(string keyId);
    }
}