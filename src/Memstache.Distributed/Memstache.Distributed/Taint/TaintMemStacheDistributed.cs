using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using MemStache.Distributed.Secure;
using MemStache.Distributed.Security;
using Microsoft.Extensions.Options;

namespace MemStache.Distributed.TaintStash
{
    public class TaintMemStacheDistributed : IMemStacheDistributed
    {
        private readonly IMemStacheDistributed _baseMemStacheDistributed;
        private readonly ITaintProvider _taintProvider;
        private readonly MemStacheOptions _options;
        private readonly ISerializer _serializer;

        public TaintMemStacheDistributed(
            IMemStacheDistributed baseMemStacheDistributed,
            ITaintProvider taintProvider,
            IOptions<MemStacheOptions> options,
            ISerializer serializer)
        {
            _baseMemStacheDistributed = baseMemStacheDistributed ?? throw new ArgumentNullException(nameof(baseMemStacheDistributed));
            _taintProvider = taintProvider ?? throw new ArgumentNullException(nameof(taintProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var taintStash = await GetTaintStashAsync<T>(key, cancellationToken);
            return taintStash != null ? taintStash.Value : default;
        }

        public async Task SetAsync<T>(string key, T value, MemStacheEntryOptions options = null, CancellationToken cancellationToken = default)
        {
            var taintStash = new TaintStash<T>(new Stash<T>(key, value), _taintProvider);
            await SetTaintStashAsync(taintStash, options, cancellationToken);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return _baseMemStacheDistributed.RemoveAsync(key, cancellationToken);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return _baseMemStacheDistributed.ExistsAsync(key, cancellationToken);
        }

        public async Task<(T Value, bool Success)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var (taintStash, success) = await TryGetTaintStashAsync<T>(key, cancellationToken);
            return (taintStash != null ? taintStash.Value : default, success);
        }

        public async Task<Stash<T>> GetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var taintStash = await GetTaintStashAsync<T>(key, cancellationToken);
            return taintStash != null ? (Stash<T>)taintStash : null;
        }

        public async Task SetStashAsync<T>(Stash<T> stash, MemStacheEntryOptions options = null, CancellationToken cancellationToken = default)
        {
            var taintStash = new TaintStash<T>(stash, _taintProvider);
            await SetTaintStashAsync(taintStash, options, cancellationToken);
        }

        public async Task<(Stash<T> Stash, bool Success)> TryGetStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var (taintStash, success) = await TryGetTaintStashAsync<T>(key, cancellationToken);
            return (taintStash != null ? (Stash<T>)taintStash : null, success);
        }

        public Task<SecureStash<T>> GetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Secure stash operations are not implemented for TaintMemStacheDistributed.");
        }

        public Task SetSecureStashAsync<T>(SecureStash<T> secureStash, MemStacheEntryOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Secure stash operations are not implemented for TaintMemStacheDistributed.");
        }

        public Task<(SecureStash<T> SecureStash, bool Success)> TryGetSecureStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Secure stash operations are not implemented for TaintMemStacheDistributed.");
        }

        private async Task<TaintStash<T>> GetTaintStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var stash = await _baseMemStacheDistributed.GetStashAsync<T>(key, cancellationToken);
            if (stash == null) return null;

            var taintStash = new TaintStash<T>(stash, _taintProvider);
            await VerifyAndDecryptTaintStashAsync(taintStash);
            return taintStash;
        }

        private async Task SetTaintStashAsync<T>(TaintStash<T> taintStash, MemStacheEntryOptions options = null, CancellationToken cancellationToken = default)
        {
            await EncryptAndSignTaintStashAsync(taintStash);
            await _baseMemStacheDistributed.SetStashAsync((Stash<T>)taintStash, options, cancellationToken);
        }

        private async Task<(TaintStash<T> TaintStash, bool Success)> TryGetTaintStashAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var (stash, success) = await _baseMemStacheDistributed.TryGetStashAsync<T>(key, cancellationToken);
            if (!success) return (null, false);

            var taintStash = new TaintStash<T>(stash, _taintProvider);
            await VerifyAndDecryptTaintStashAsync(taintStash);
            return (taintStash, true);
        }

        private async Task EncryptAndSignTaintStashAsync<T>(TaintStash<T> taintStash)
        {
            var taintSignature = await _taintProvider.GenerateTaintSignatureAsync(_options.DefaultMasterKeyId);
            taintStash.TaintSignature = taintSignature;
            taintStash.TargetProfile = await _taintProvider.GenerateCompilationTargetProfileAsync();

            var serializedValue = _serializer.Serialize(taintStash.Value);
            var encryptedValue = await _taintProvider.EncryptWithTaintAsync(_options.DefaultMasterKeyId, serializedValue, taintSignature);
            taintStash.Value = _serializer.Deserialize<T>(encryptedValue);
        }

        private async Task VerifyAndDecryptTaintStashAsync<T>(TaintStash<T> taintStash)
        {
            if (!await _taintProvider.VerifyTaintSignatureAsync(_options.DefaultMasterKeyId, taintStash.TaintSignature))
            {
                throw new SecurityException("Taint signature verification failed");
            }

            if (!await _taintProvider.VerifyCompilationTargetProfileAsync(taintStash.TargetProfile))
            {
                throw new SecurityException("Compilation target profile verification failed");
            }

            var serializedValue = _serializer.Serialize(taintStash.Value);
            var (decryptedValue, extractedTaintSignature) = await _taintProvider.DecryptWithTaintAsync(_options.DefaultMasterKeyId, serializedValue);
            taintStash.Value = _serializer.Deserialize<T>(decryptedValue);
            taintStash.TaintSignature = extractedTaintSignature;
        }
    }
}
