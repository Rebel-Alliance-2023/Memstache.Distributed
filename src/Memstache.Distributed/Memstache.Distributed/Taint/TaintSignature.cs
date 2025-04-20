using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemStache.Distributed.Security;

namespace MemStache.Distributed.TaintStash
{
    public class TaintSignature
    {
        public string SignatureId { get; private set; }
        public Dictionary<string, string> HardwareTraits { get; private set; }
        public Dictionary<string, string> SoftwareTraits { get; private set; }
        public Dictionary<string, string> EnvironmentalConstraints { get; private set; }
        public byte[] Signature { get; private set; }

        private readonly ITaintProvider _taintProvider;

        public TaintSignature(ITaintProvider taintProvider)
        {
            _taintProvider = taintProvider ?? throw new ArgumentNullException(nameof(taintProvider));
            InitializeProperties();
        }

        private void InitializeProperties()
        {
            SignatureId = Guid.NewGuid().ToString();
            HardwareTraits = new Dictionary<string, string>();
            SoftwareTraits = new Dictionary<string, string>();
            EnvironmentalConstraints = new Dictionary<string, string>();
        }

        public async Task GenerateAsync(string keyId)
        {
            CollectTraitsAndConstraints();
            string dataToSign = SerializeTraitsAndConstraints();
            var taintSignature = await _taintProvider.GenerateTaintSignatureAsync(keyId);
            this.Signature = taintSignature.Signature;
        }

        public async Task<bool> VerifyAsync(string keyId)
        {
            return await _taintProvider.VerifyTaintSignatureAsync(keyId, this);
        }

        public void AddHardwareTrait(string traitName, string traitValue)
        {
            HardwareTraits[traitName] = traitValue;
        }

        public void AddSoftwareTrait(string traitName, string traitValue)
        {
            SoftwareTraits[traitName] = traitValue;
        }

        public void AddEnvironmentalConstraint(string constraintName, string constraintValue)
        {
            EnvironmentalConstraints[constraintName] = constraintValue;
        }

        public bool VerifyEnvironmentalConstraints()
        {
            return EnvironmentalConstraints.All(constraint => VerifyConstraint(constraint.Key, constraint.Value));
        }

        private void CollectTraitsAndConstraints()
        {
            CollectHardwareTraits();
            CollectSoftwareTraits();
            CollectEnvironmentalConstraints();
        }

        private void CollectHardwareTraits()
        {
            AddHardwareTrait("ProcessorCount", Environment.ProcessorCount.ToString());
            AddHardwareTrait("Is64BitOperatingSystem", Environment.Is64BitOperatingSystem.ToString());
            AddHardwareTrait("Is64BitProcess", Environment.Is64BitProcess.ToString());
            // Add more hardware traits as needed
        }

        private void CollectSoftwareTraits()
        {
            AddSoftwareTrait("OSVersion", Environment.OSVersion.ToString());
            AddSoftwareTrait("DotNetVersion", Environment.Version.ToString());
            AddSoftwareTrait("MachineName", Environment.MachineName);
            // Add more software traits as needed
        }

        private void CollectEnvironmentalConstraints()
        {
            AddEnvironmentalConstraint("CurrentDirectory", Environment.CurrentDirectory);
            AddEnvironmentalConstraint("SystemDirectory", Environment.SystemDirectory);
            AddEnvironmentalConstraint("UserDomainName", Environment.UserDomainName);
            // Add more environmental constraints as needed
        }

        private string SerializeTraitsAndConstraints()
        {
            var sb = new StringBuilder();
            foreach (var trait in HardwareTraits.Concat(SoftwareTraits).Concat(EnvironmentalConstraints))
            {
                sb.AppendFormat("{0}:{1};", trait.Key, trait.Value);
            }
            return sb.ToString();
        }

        private bool VerifyConstraint(string constraintName, string constraintValue)
        {
            switch (constraintName)
            {
                case "CurrentDirectory":
                    return Environment.CurrentDirectory == constraintValue;
                case "SystemDirectory":
                    return Environment.SystemDirectory == constraintValue;
                case "UserDomainName":
                    return Environment.UserDomainName == constraintValue;
                // Add more constraint verifications as needed
                default:
                    return true; // Unknown constraints are considered valid
            }
        }

        public static async Task<TaintSignature> CombineAsync(TaintSignature signature1, TaintSignature signature2)
        {
            var combinedSignature = new TaintSignature(signature1._taintProvider);

            CombineDictionaries(combinedSignature.HardwareTraits, signature1.HardwareTraits, signature2.HardwareTraits);
            CombineDictionaries(combinedSignature.SoftwareTraits, signature1.SoftwareTraits, signature2.SoftwareTraits);
            CombineDictionaries(combinedSignature.EnvironmentalConstraints, signature1.EnvironmentalConstraints, signature2.EnvironmentalConstraints);

            await combinedSignature.GenerateAsync(signature1.SignatureId + "_" + signature2.SignatureId);

            return combinedSignature;
        }

        private static void CombineDictionaries(Dictionary<string, string> target, Dictionary<string, string> source1, Dictionary<string, string> source2)
        {
            foreach (var kvp in source1.Concat(source2))
            {
                if (!target.ContainsKey(kvp.Key))
                {
                    target[kvp.Key] = kvp.Value;
                }
                else
                {
                    target[kvp.Key] = CompareValues(target[kvp.Key], kvp.Value);
                }
            }
        }

        private static string CompareValues(string value1, string value2)
        {
            if (int.TryParse(value1, out int intValue1) && int.TryParse(value2, out int intValue2))
            {
                return Math.Max(intValue1, intValue2).ToString();
            }
            return value1.CompareTo(value2) > 0 ? value1 : value2;
        }

        public async Task<(TaintSignature, TaintSignature)> SplitAsync()
        {
            var signature1 = new TaintSignature(_taintProvider);
            var signature2 = new TaintSignature(_taintProvider);

            SplitDictionary(this.HardwareTraits, signature1.HardwareTraits, signature2.HardwareTraits);
            SplitDictionary(this.SoftwareTraits, signature1.SoftwareTraits, signature2.SoftwareTraits);
            SplitDictionary(this.EnvironmentalConstraints, signature1.EnvironmentalConstraints, signature2.EnvironmentalConstraints);

            await signature1.GenerateAsync(this.SignatureId + "_1");
            await signature2.GenerateAsync(this.SignatureId + "_2");

            return (signature1, signature2);
        }

        private void SplitDictionary(Dictionary<string, string> source, Dictionary<string, string> target1, Dictionary<string, string> target2)
        {
            foreach (var kvp in source)
            {
                if (new Random().Next(2) == 0)
                {
                    target1[kvp.Key] = kvp.Value;
                }
                else
                {
                    target2[kvp.Key] = kvp.Value;
                }
            }
        }

        public override string ToString()
        {
            return $"SignatureId: {SignatureId}, Hardware Traits: {HardwareTraits.Count}, Software Traits: {SoftwareTraits.Count}, Environmental Constraints: {EnvironmentalConstraints.Count}";
        }
    }
}
