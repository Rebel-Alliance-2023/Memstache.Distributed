using System;
using System.Text;
using System.Threading.Tasks;
using MemStache.Distributed.Security;
using NBitcoin;

namespace MemStache.Distributed.TaintStash
{
    public class TaintCryptoService : ICryptoService
    {
        private readonly ICryptoService _baseCryptoService;
        private readonly HDKeyManager _hdKeyManager;
        private readonly ITaintProvider _taintProvider;

        public TaintCryptoService(ICryptoService baseCryptoService, HDKeyManager hdKeyManager, ITaintProvider taintProvider)
        {
            _baseCryptoService = baseCryptoService ?? throw new ArgumentNullException(nameof(baseCryptoService));
            _hdKeyManager = hdKeyManager ?? throw new ArgumentNullException(nameof(hdKeyManager));
            _taintProvider = taintProvider ?? throw new ArgumentNullException(nameof(taintProvider));
        }

        public async Task<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairFromMnemonicAsync(string mnemonic)
        {
            return await _baseCryptoService.GenerateKeyPairFromMnemonicAsync(mnemonic);
        }

        public async Task<byte[]> EncryptDataAsync(byte[] publicKey, byte[] data)
        {
            var taintSignature = await CreateTaintSignatureAsync(Convert.ToBase64String(publicKey));
            var dataWithTaint = CombineDataAndTaint(data, Encoding.UTF8.GetBytes(taintSignature.SignatureId));
            return await _baseCryptoService.EncryptDataAsync(publicKey, dataWithTaint);
        }

        public async Task<byte[]> DecryptDataAsync(byte[] privateKey, byte[] encryptedData)
        {
            var decryptedDataWithTaint = await _baseCryptoService.DecryptDataAsync(privateKey, encryptedData);
            return ExtractDataFromTaint(decryptedDataWithTaint);
        }

        public async Task<byte[]> SignDataAsync(byte[] privateKey, byte[] data)
        {
            var taintSignature = await CreateTaintSignatureAsync(Convert.ToBase64String(privateKey));
            var dataWithTaint = CombineDataAndTaint(data, Encoding.UTF8.GetBytes(taintSignature.SignatureId));
            return await _baseCryptoService.SignDataAsync(privateKey, dataWithTaint);
        }

        public async Task<bool> VerifyDataAsync(byte[] publicKey, byte[] data, byte[] signature)
        {
            var taintSignature = await CreateTaintSignatureAsync(Convert.ToBase64String(publicKey));
            var dataWithTaint = CombineDataAndTaint(data, Encoding.UTF8.GetBytes(taintSignature.SignatureId));
            return await _baseCryptoService.VerifyDataAsync(publicKey, dataWithTaint, signature);
        }

        public async Task<byte[]> EncryptWithTaintAsync(string keyId, byte[] data, TaintSignature taintSignature)
        {
            var key = await _hdKeyManager.GetKeyByIdAsync(keyId);
            var dataWithTaint = CombineDataAndTaint(data, Encoding.UTF8.GetBytes(taintSignature.SignatureId));
            return await _baseCryptoService.EncryptDataAsync(key.PrivateKey.PubKey.ToBytes(), dataWithTaint);
        }

        public async Task<(byte[] DecryptedData, TaintSignature ExtractedTaintSignature)> DecryptWithTaintAsync(string keyId, byte[] encryptedData)
        {
            var key = await _hdKeyManager.GetKeyByIdAsync(keyId);
            var decryptedDataWithTaint = await _baseCryptoService.DecryptDataAsync(key.PrivateKey.ToBytes(), encryptedData);
            var decryptedData = ExtractDataFromTaint(decryptedDataWithTaint);
            var taintSignatureId = Encoding.UTF8.GetString(decryptedDataWithTaint, 4, BitConverter.ToInt32(decryptedDataWithTaint, 0));
            var extractedTaintSignature = new TaintSignature(_taintProvider);
            await extractedTaintSignature.GenerateAsync(taintSignatureId);
            return (decryptedData, extractedTaintSignature);
        }

        private async Task<TaintSignature> CreateTaintSignatureAsync(string keyIdentifier)
        {
            var taintSignature = new TaintSignature(_taintProvider);
            await taintSignature.GenerateAsync(keyIdentifier);
            return taintSignature;
        }

        private byte[] CombineDataAndTaint(byte[] data, byte[] taint)
        {
            var combined = new byte[data.Length + taint.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(taint.Length), 0, combined, 0, 4);
            Buffer.BlockCopy(taint, 0, combined, 4, taint.Length);
            Buffer.BlockCopy(data, 0, combined, taint.Length + 4, data.Length);
            return combined;
        }

        private byte[] ExtractDataFromTaint(byte[] dataWithTaint)
        {
            var taintLength = BitConverter.ToInt32(dataWithTaint, 0);
            var dataLength = dataWithTaint.Length - taintLength - 4;
            var data = new byte[dataLength];
            Buffer.BlockCopy(dataWithTaint, taintLength + 4, data, 0, dataLength);
            return data;
        }

        // Synchronous methods implementation (calling async methods)
        public string GenerateMnemonic()
        {
            return GenerateMnemonicAsync().GetAwaiter().GetResult();
        }

        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromMnemonic(string mnemonic)
        {
            return GenerateKeyPairFromMnemonicAsync(mnemonic).GetAwaiter().GetResult();
        }

        public byte[] EncryptData(byte[] publicKey, byte[] data)
        {
            return EncryptDataAsync(publicKey, data).GetAwaiter().GetResult();
        }

        public byte[] DecryptData(byte[] privateKey, byte[] encryptedData)
        {
            return DecryptDataAsync(privateKey, encryptedData).GetAwaiter().GetResult();
        }

        public byte[] SignData(byte[] privateKey, byte[] data)
        {
            return SignDataAsync(privateKey, data).GetAwaiter().GetResult();
        }

        public bool VerifyData(byte[] publicKey, byte[] data, byte[] signature)
        {
            return VerifyDataAsync(publicKey, data, signature).GetAwaiter().GetResult();
        }

        public Task<string> GenerateMnemonicAsync()
        {
            throw new NotImplementedException();
        }
    }
}
