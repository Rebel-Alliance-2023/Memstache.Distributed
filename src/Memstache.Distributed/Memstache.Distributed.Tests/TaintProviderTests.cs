using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MemStache.Distributed.TaintStash;
using MemStache.Distributed.Security;
using Microsoft.Extensions.Options;
using NBitcoin;

namespace MemStache.Distributed.Tests
{
    public class TaintProviderTests
    {
        private readonly Mock<TaintCryptoService> _mockCryptoService;
        private readonly Mock<TaintKeyManagementService> _mockKeyManagementService;
        private readonly Mock<HDKeyManager> _mockHdKeyManager;
        private readonly TaintProvider _taintProvider;

        public TaintProviderTests()
        {
            _mockCryptoService = new Mock<TaintCryptoService>(MockBehavior.Strict);
            _mockKeyManagementService = new Mock<TaintKeyManagementService>(MockBehavior.Strict);
            _mockHdKeyManager = new Mock<HDKeyManager>(MockBehavior.Strict);
            _taintProvider = new TaintProvider(_mockCryptoService.Object, _mockKeyManagementService.Object, _mockHdKeyManager.Object);
        }

        [Fact]
        public async Task GenerateTaintSignatureAsync_ShouldReturnValidSignature()
        {
            // Arrange
            string keyId = "testKey";
            var expectedSignature = new byte[] { 1, 2, 3, 4, 5 };
            _mockCryptoService.Setup(cs => cs.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(expectedSignature);

            // Act
            var result = await _taintProvider.GenerateTaintSignatureAsync(keyId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSignature, result.Signature);
            Assert.NotEmpty(result.HardwareTraits);
            Assert.NotEmpty(result.SoftwareTraits);
            Assert.NotEmpty(result.EnvironmentalConstraints);
        }

        [Fact]
        public async Task VerifyTaintSignatureAsync_ShouldReturnTrueForValidSignature()
        {
            // Arrange
            string keyId = "testKey";
            var taintSignature = new TaintSignature(_taintProvider);
            await taintSignature.GenerateAsync(keyId);
            _mockCryptoService.Setup(cs => cs.VerifyDataAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(true);

            // Act
            var result = await _taintProvider.VerifyTaintSignatureAsync(keyId, taintSignature);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CombineTaintSignaturesAsync_ShouldReturnCombinedSignature()
        {
            // Arrange
            string keyId1 = "testKey1";
            string keyId2 = "testKey2";
            var signature1 = await _taintProvider.GenerateTaintSignatureAsync(keyId1);
            var signature2 = await _taintProvider.GenerateTaintSignatureAsync(keyId2);

            // Act
            var combinedSignature = await _taintProvider.CombineTaintSignaturesAsync(signature1, signature2, "combinedKey");

            // Assert
            Assert.NotNull(combinedSignature);
            Assert.True(combinedSignature.HardwareTraits.Count >= signature1.HardwareTraits.Count);
            Assert.True(combinedSignature.SoftwareTraits.Count >= signature1.SoftwareTraits.Count);
            Assert.True(combinedSignature.EnvironmentalConstraints.Count >= signature1.EnvironmentalConstraints.Count);
        }

        [Fact]
        public async Task EncryptWithTaintAsync_ShouldReturnEncryptedData()
        {
            // Arrange
            string keyId = "testKey";
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            var taintSignature = await _taintProvider.GenerateTaintSignatureAsync(keyId);
            var expectedEncryptedData = new byte[] { 5, 4, 3, 2, 1 };
            _mockCryptoService.Setup(cs => cs.EncryptWithTaintAsync(keyId, data, taintSignature))
                .ReturnsAsync(expectedEncryptedData);

            // Act
            var encryptedData = await _taintProvider.EncryptWithTaintAsync(keyId, data, taintSignature);

            // Assert
            Assert.Equal(expectedEncryptedData, encryptedData);
        }

        [Fact]
        public async Task DecryptWithTaintAsync_ShouldReturnDecryptedDataAndExtractedSignature()
        {
            // Arrange
            string keyId = "testKey";
            byte[] encryptedData = new byte[] { 5, 4, 3, 2, 1 };
            byte[] expectedDecryptedData = new byte[] { 1, 2, 3, 4, 5 };
            var expectedExtractedSignature = new TaintSignature(_taintProvider);
            _mockCryptoService.Setup(cs => cs.DecryptWithTaintAsync(keyId, encryptedData))
                .ReturnsAsync((expectedDecryptedData, expectedExtractedSignature));

            // Act
            var (decryptedData, extractedSignature) = await _taintProvider.DecryptWithTaintAsync(keyId, encryptedData);

            // Assert
            Assert.Equal(expectedDecryptedData, decryptedData);
            Assert.Equal(expectedExtractedSignature, extractedSignature);
        }

        [Fact]
        public async Task GenerateCompilationTargetProfileAsync_ShouldReturnValidProfile()
        {
            // Act
            var profile = await _taintProvider.GenerateCompilationTargetProfileAsync();

            // Assert
            Assert.NotNull(profile);
            Assert.NotEmpty(profile.HardwareRequirements);
            Assert.NotEmpty(profile.SoftwareRequirements);
            Assert.NotEmpty(profile.EnvironmentalConstraints);
        }

        [Fact]
        public async Task VerifyCompilationTargetProfileAsync_ShouldReturnTrueForCurrentEnvironment()
        {
            // Arrange
            var profile = await _taintProvider.GenerateCompilationTargetProfileAsync();

            // Act
            var result = await _taintProvider.VerifyCompilationTargetProfileAsync(profile);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateKeyTaintAsync_ShouldUpdateTaintSignature()
        {
            // Arrange
            string keyId = "testKey";
            var newTaintSignature = await _taintProvider.GenerateTaintSignatureAsync(keyId);
            _mockKeyManagementService.Setup(kms => kms.UpdateKeyTaintAsync(keyId, newTaintSignature))
                .Returns(Task.CompletedTask);

            // Act
            await _taintProvider.UpdateKeyTaintAsync(keyId, newTaintSignature);

            // Assert
            _mockKeyManagementService.Verify(kms => kms.UpdateKeyTaintAsync(keyId, newTaintSignature), Times.Once);
        }

        [Fact]
        public async Task GetKeyTaintAsync_ShouldReturnTaintSignature()
        {
            // Arrange
            string keyId = "testKey";
            var expectedTaintSignature = new TaintSignature(_taintProvider);
            _mockKeyManagementService.Setup(kms => kms.GetTaintSignatureAsync(keyId))
                .ReturnsAsync(expectedTaintSignature);

            // Act
            var result = await _taintProvider.GetKeyTaintAsync(keyId);

            // Assert
            Assert.Equal(expectedTaintSignature, result);
        }
    }

    public class TaintStashTests
    {
        private readonly Mock<ITaintProvider> _mockTaintProvider;

        public TaintStashTests()
        {
            _mockTaintProvider = new Mock<ITaintProvider>(MockBehavior.Strict);
        }

        [Fact]
        public async Task CoalesceAsync_ShouldCombineTaintStashes()
        {
            // Arrange
            var stash1 = new TaintStash<string>(new Stash<string>("key1", "value1"), _mockTaintProvider.Object);
            var stash2 = new TaintStash<string>(new Stash<string>("key2", "value2"), _mockTaintProvider.Object);

            _mockTaintProvider.Setup(tp => tp.CombineTaintSignaturesAsync(It.IsAny<TaintSignature>(), It.IsAny<TaintSignature>(), It.IsAny<string>()))
                .ReturnsAsync(new TaintSignature(_mockTaintProvider.Object));

            // Act
            var result = await stash1.CoalesceAsync(stash2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("key1", result.Key);
            Assert.Equal("value1", result.Value);
            _mockTaintProvider.Verify(tp => tp.CombineTaintSignaturesAsync(It.IsAny<TaintSignature>(), It.IsAny<TaintSignature>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DecomposeAsync_ShouldSplitTaintStash()
        {
            // Arrange
            var stash = new TaintStash<string>(new Stash<string>("key", "value"), _mockTaintProvider.Object);

            _mockTaintProvider.Setup(tp => tp.GenerateTaintSignatureAsync(It.IsAny<string>()))
                .ReturnsAsync(new TaintSignature(_mockTaintProvider.Object));

            // Act
            var (stash1, stash2) = await stash.DecomposeAsync();

            // Assert
            Assert.NotNull(stash1);
            Assert.NotNull(stash2);
            Assert.StartsWith("key", stash1.Key);
            Assert.StartsWith("key", stash2.Key);
            Assert.Equal("value", stash1.Value);
            Assert.Equal("value", stash2.Value);
        }

        [Fact]
        public async Task VerifyEnvironmentalConstraintsAsync_ShouldVerifyConstraints()
        {
            // Arrange
            var stash = new TaintStash<string>(new Stash<string>("key", "value"), _mockTaintProvider.Object);
            _mockTaintProvider.Setup(tp => tp.VerifyCompilationTargetProfileAsync(It.IsAny<CompilationTargetProfile>()))
                .ReturnsAsync(true);
            _mockTaintProvider.Setup(tp => tp.VerifyTaintSignatureAsync(It.IsAny<string>(), It.IsAny<TaintSignature>()))
                .ReturnsAsync(true);

            // Act
            var result = await stash.VerifyEnvironmentalConstraintsAsync();

            // Assert
            Assert.True(result);
            _mockTaintProvider.Verify(tp => tp.VerifyCompilationTargetProfileAsync(It.IsAny<CompilationTargetProfile>()), Times.Once);
            _mockTaintProvider.Verify(tp => tp.VerifyTaintSignatureAsync(It.IsAny<string>(), It.IsAny<TaintSignature>()), Times.Once);
        }
    }

    public class TwoDimensionalProvenanceTests
    {
        [Fact]
        public void Combine_ShouldCombineProvenanceInformation()
        {
            // Arrange
            var provenance1 = new TwoDimensionalProvenance(TwoDimensionalProvenance.SecurityLevel.Confidential, "Agency1");
            var provenance2 = new TwoDimensionalProvenance(TwoDimensionalProvenance.SecurityLevel.Secret, "Agency2");

            // Act
            var combinedProvenance = provenance1.Combine(provenance2);

            // Assert
            Assert.Equal(TwoDimensionalProvenance.SecurityLevel.Secret, combinedProvenance.Level);
            Assert.Contains("Agency1", combinedProvenance.AgencyContext);
            Assert.Contains("Agency2", combinedProvenance.AgencyContext);
        }

        [Fact]
        public void Split_ShouldSplitProvenanceInformation()
        {
            // Arrange
            var provenance = new TwoDimensionalProvenance(TwoDimensionalProvenance.SecurityLevel.TopSecret, "Agency1|Agency2");
            provenance.AddMetadata("Key1", "Value1");
            provenance.AddMetadata("Key2", "Value2");

            // Act
            var (provenance1, provenance2) = provenance.Split();

            // Assert
            Assert.Equal(TwoDimensionalProvenance.SecurityLevel.TopSecret, provenance1.Level);
            Assert.Equal(TwoDimensionalProvenance.SecurityLevel.TopSecret, provenance2.Level);
            Assert.True(provenance1.AgencyContext == "Agency1" || provenance1.AgencyContext == "Agency2");
            Assert.True(provenance2.AgencyContext == "Agency1" || provenance2.AgencyContext == "Agency2");
            Assert.NotEqual(provenance1.AgencyContext, provenance2.AgencyContext);
            Assert.True(provenance1.AdditionalMetadata.Count + provenance2.AdditionalMetadata.Count == 2);
        }
    }

    public class TaintMemStacheDistributedTests
    {
        private readonly Mock<IMemStacheDistributed> _mockBaseDistributed;
        private readonly Mock<ITaintProvider> _mockTaintProvider;
        private readonly IOptions<MemStacheOptions> _options;
        private readonly Mock<ISerializer> _mockSerializer;
        private readonly TaintMemStacheDistributed _taintMemStache;

        public TaintMemStacheDistributedTests()
        {
            _mockBaseDistributed = new Mock<IMemStacheDistributed>(MockBehavior.Strict);
            _mockTaintProvider = new Mock<ITaintProvider>(MockBehavior.Strict);
            _mockSerializer = new Mock<ISerializer>(MockBehavior.Strict);

            // Create actual options instead of mocking
            _options = Options.Create(new MemStacheOptions());

            _taintMemStache = new TaintMemStacheDistributed(
                _mockBaseDistributed.Object,
                _mockTaintProvider.Object,
                _options,
                _mockSerializer.Object
            );
        }


        [Fact]
        public async Task SetAsync_ShouldEncryptAndSignData()
        {
            // Arrange
            string key = "testKey";
            string value = "testValue";
            var taintSignature = new TaintSignature(_mockTaintProvider.Object);
            var compilationProfile = new CompilationTargetProfile();

            _mockTaintProvider.Setup(tp => tp.GenerateTaintSignatureAsync(It.IsAny<string>()))
                .ReturnsAsync(taintSignature);
            _mockTaintProvider.Setup(tp => tp.GenerateCompilationTargetProfileAsync())
                .ReturnsAsync(compilationProfile);
            _mockTaintProvider.Setup(tp => tp.EncryptWithTaintAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<TaintSignature>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns(new byte[] { 4, 5, 6 });
            _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<byte[]>())).Returns(value);
            _mockBaseDistributed.Setup(bd => bd.SetStashAsync(It.IsAny<Stash<string>>(), null, default))
                .Returns(Task.CompletedTask);

            // Act
            await _taintMemStache.SetAsync(key, value);

            // Assert
            _mockTaintProvider.Verify(tp => tp.GenerateTaintSignatureAsync(It.IsAny<string>()), Times.Once);
            _mockTaintProvider.Verify(tp => tp.GenerateCompilationTargetProfileAsync(), Times.Once);
            _mockTaintProvider.Verify(tp => tp.EncryptWithTaintAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<TaintSignature>()), Times.Once);
            _mockBaseDistributed.Verify(bd => bd.SetStashAsync(It.IsAny<Stash<string>>(), null, default), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ShouldDecryptAndVerifyData()
        {
            // Arrange
            string key = "testKey";
            string expectedValue = "testValue";
            var taintSignature = new TaintSignature(_mockTaintProvider.Object);
            var compilationProfile = new CompilationTargetProfile();

            _mockBaseDistributed.Setup(bd => bd.GetStashAsync<string>(key, default))
                .ReturnsAsync(new Stash<string>(key, expectedValue));
            _mockTaintProvider.Setup(tp => tp.VerifyTaintSignatureAsync(It.IsAny<string>(), It.IsAny<TaintSignature>()))
                .ReturnsAsync(true);
            _mockTaintProvider.Setup(tp => tp.VerifyCompilationTargetProfileAsync(It.IsAny<CompilationTargetProfile>()))
                .ReturnsAsync(true);
            _mockTaintProvider.Setup(tp => tp.DecryptWithTaintAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
                .ReturnsAsync((new byte[] { 1, 2, 3 }, taintSignature));
            _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns(new byte[] { 4, 5, 6 });
            _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<byte[]>())).Returns(expectedValue);

            // Act
            var result = await _taintMemStache.GetAsync<string>(key);

            // Assert
            Assert.Equal(expectedValue, result);
            _mockTaintProvider.Verify(tp => tp.VerifyTaintSignatureAsync(It.IsAny<string>(), It.IsAny<TaintSignature>()), Times.Once);
            _mockTaintProvider.Verify(tp => tp.VerifyCompilationTargetProfileAsync(It.IsAny<CompilationTargetProfile>()), Times.Once);
            _mockTaintProvider.Verify(tp => tp.DecryptWithTaintAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
        }
    }

    public class HDKeyManagerTests
    {
        private readonly HDKeyManager _hdKeyManager;
        private readonly Mock<ICryptoService> _mockCryptoService;

        public HDKeyManagerTests()
        {
            _mockCryptoService = new Mock<ICryptoService>(MockBehavior.Strict);
            _hdKeyManager = new HDKeyManager(_mockCryptoService.Object);
        }

        [Fact]
        public async Task CreateMasterKeyAsync_ShouldReturnValidMasterKey()
        {
            // Arrange
            var mnemonic = "test mnemonic";
            var keyPair = (new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });
            _mockCryptoService.Setup(cs => cs.GenerateMnemonicAsync()).ReturnsAsync(mnemonic);
            _mockCryptoService.Setup(cs => cs.GenerateKeyPairFromMnemonicAsync(mnemonic)).ReturnsAsync(keyPair);

            // Act
            var result = await _hdKeyManager.CreateMasterKeyAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            _mockCryptoService.Verify(cs => cs.GenerateMnemonicAsync(), Times.Once);
            _mockCryptoService.Verify(cs => cs.GenerateKeyPairFromMnemonicAsync(mnemonic), Times.Once);
        }

        [Fact]
        public async Task DeriveLowerLevelKeyAsync_ShouldReturnDerivedKey()
        {
            // Arrange
            string parentKeyId = "parentKey";
            int targetLevel = 3;
            var parentKey = new Mock<ExtKey>();
            var derivedKey = new Mock<ExtKey>();
            parentKey.Setup(pk => pk.Derive(It.IsAny<KeyPath>())).Returns(derivedKey.Object);

            // Act
            var result = await _hdKeyManager.DeriveLowerLevelKeyAsync(parentKeyId, targetLevel);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetKeyByLevelAsync_ShouldReturnKeyForGivenLevel()
        {
            // Arrange
            int level = 3;

            // Act
            var result = await _hdKeyManager.GetKeyByLevelAsync(level);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetKeyByIdAsync_ShouldReturnKeyForGivenId()
        {
            // Arrange
            string keyId = "testKey";

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _hdKeyManager.GetKeyByIdAsync(keyId));
        }
    }

    public class CompilationTargetProfileTests
    {
        [Fact]
        public void Verify_ShouldReturnTrueForValidProfile()
        {
            // Arrange
            var profile = CompilationTargetProfile.GenerateFromCurrentEnvironment();

            // Act
            var result = profile.Verify();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Combine_ShouldReturnCombinedProfile()
        {
            // Arrange
            var profile1 = new CompilationTargetProfile();
            profile1.AddHardwareRequirement("CPU", "4 cores");
            var profile2 = new CompilationTargetProfile();
            profile2.AddHardwareRequirement("RAM", "8 GB");

            // Act
            var combinedProfile = CompilationTargetProfile.Combine(profile1, profile2);

            // Assert
            Assert.Contains("CPU", combinedProfile.HardwareRequirements);
            Assert.Contains("RAM", combinedProfile.HardwareRequirements);
        }

        [Fact]
        public void Split_ShouldReturnTwoSeparateProfiles()
        {
            // Arrange
            var profile = new CompilationTargetProfile();
            profile.AddHardwareRequirement("CPU", "4 cores");
            profile.AddHardwareRequirement("RAM", "8 GB");
            profile.AddSoftwareRequirement("OS", "Windows 10");

            // Act
            var (profile1, profile2) = profile.Split();

            // Assert
            Assert.NotEmpty(profile1.HardwareRequirements);
            Assert.NotEmpty(profile2.HardwareRequirements);
            Assert.True(profile1.HardwareRequirements.Count + profile2.HardwareRequirements.Count == 2);
            Assert.Contains("OS", profile1.SoftwareRequirements.Keys.Concat(profile2.SoftwareRequirements.Keys));
        }
    }
}