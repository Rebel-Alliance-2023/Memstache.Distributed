using System;
using System.Threading.Tasks;

namespace MemStache.Distributed.TaintStash
{
    public class TaintProvider : ITaintProvider
    {
        private readonly TaintCryptoService _cryptoService;
        private readonly TaintKeyManagementService _keyManagementService;
        private readonly HDKeyManager _hdKeyManager;

        public TaintProvider(TaintCryptoService cryptoService, TaintKeyManagementService keyManagementService, HDKeyManager hdKeyManager)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
            _hdKeyManager = hdKeyManager ?? throw new ArgumentNullException(nameof(hdKeyManager));
        }

        public async Task<TaintSignature> GenerateTaintSignatureAsync(string keyId)
        {
            var taintSignature = new TaintSignature(this);
            await taintSignature.GenerateAsync(keyId);
            return taintSignature;
        }

        public async Task<bool> VerifyTaintSignatureAsync(string keyId, TaintSignature taintSignature)
        {
            return await taintSignature.VerifyAsync(keyId);
        }

        public async Task<TaintSignature> CombineTaintSignaturesAsync(TaintSignature signature1, TaintSignature signature2, string keyId)
        {
            var combinedSignature = await TaintSignature.CombineAsync(signature1, signature2);
            await combinedSignature.GenerateAsync(keyId);
            return combinedSignature;
        }

        public async Task<byte[]> EncryptWithTaintAsync(string keyId, byte[] data, TaintSignature taintSignature)
        {
            return await _cryptoService.EncryptWithTaintAsync(keyId, data, taintSignature);
        }

        public async Task<(byte[] DecryptedData, TaintSignature ExtractedTaintSignature)> DecryptWithTaintAsync(string keyId, byte[] encryptedData)
        {
            return await _cryptoService.DecryptWithTaintAsync(keyId, encryptedData);
        }

        public Task<CompilationTargetProfile> GenerateCompilationTargetProfileAsync()
        {
            return Task.FromResult(CompilationTargetProfile.GenerateFromCurrentEnvironment());
        }

        public Task<bool> VerifyCompilationTargetProfileAsync(CompilationTargetProfile profile)
        {
            return Task.FromResult(profile.Verify());
        }

        public async Task UpdateKeyTaintAsync(string keyId, TaintSignature newTaintSignature)
        {
            await _keyManagementService.UpdateKeyTaintAsync(keyId, newTaintSignature);
        }

        public async Task<TaintSignature> GetKeyTaintAsync(string keyId)
        {
            return await _keyManagementService.GetTaintSignatureAsync(keyId);
        }
    }
}
