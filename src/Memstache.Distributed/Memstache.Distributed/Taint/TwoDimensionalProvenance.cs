using System;
using System.Collections.Generic;
using System.Linq;

namespace MemStache.Distributed.TaintStash
{
    public class TwoDimensionalProvenance
    {
        public enum SecurityLevel
        {
            Unclassified = 0,
            Confidential = 1,
            Secret = 2,
            TopSecret = 3
        }

        public SecurityLevel Level { get; private set; }
        public string AgencyContext { get; private set; }
        public Dictionary<string, string> AdditionalMetadata { get; private set; }
        public string ProvenanceId { get; private set; }

        public TwoDimensionalProvenance(SecurityLevel level = SecurityLevel.Unclassified, string agencyContext = "")
        {
            ProvenanceId = Guid.NewGuid().ToString();
            Level = level;
            AgencyContext = agencyContext;
            AdditionalMetadata = new Dictionary<string, string>();
        }

        public void SetSecurityLevel(SecurityLevel level)
        {
            Level = level;
        }

        public void SetAgencyContext(string agencyContext)
        {
            if (string.IsNullOrWhiteSpace(agencyContext))
                throw new ArgumentException("Agency context cannot be null or empty", nameof(agencyContext));

            AgencyContext = agencyContext;
        }

        public void AddMetadata(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));

            AdditionalMetadata[key] = value;
        }

        public bool TryGetMetadata(string key, out string value)
        {
            return AdditionalMetadata.TryGetValue(key, out value);
        }

        public TwoDimensionalProvenance Combine(TwoDimensionalProvenance other)
        {
            var combinedProvenance = new TwoDimensionalProvenance
            {
                Level = (SecurityLevel)Math.Max((int)this.Level, (int)other.Level),
                AgencyContext = CombineAgencyContexts(this.AgencyContext, other.AgencyContext)
            };

            foreach (var metadata in this.AdditionalMetadata)
            {
                combinedProvenance.AddMetadata(metadata.Key, metadata.Value);
            }

            foreach (var metadata in other.AdditionalMetadata)
            {
                if (!combinedProvenance.AdditionalMetadata.ContainsKey(metadata.Key))
                {
                    combinedProvenance.AddMetadata(metadata.Key, metadata.Value);
                }
                else
                {
                    combinedProvenance.AddMetadata($"{metadata.Key}_Conflict", metadata.Value);
                }
            }

            return combinedProvenance;
        }

        private string CombineAgencyContexts(string context1, string context2)
        {
            if (string.IsNullOrWhiteSpace(context1)) return context2;
            if (string.IsNullOrWhiteSpace(context2)) return context1;
            return $"{context1}|{context2}";
        }

        public (TwoDimensionalProvenance, TwoDimensionalProvenance) Split()
        {
            var provenance1 = new TwoDimensionalProvenance();
            var provenance2 = new TwoDimensionalProvenance();

            // Split security level
            provenance1.SetSecurityLevel(this.Level);
            provenance2.SetSecurityLevel(this.Level);

            // Split agency context
            var contexts = this.AgencyContext.Split('|');
            provenance1.SetAgencyContext(contexts.FirstOrDefault() ?? "");
            provenance2.SetAgencyContext(contexts.LastOrDefault() ?? "");

            // Split additional metadata
            SplitMetadata(this.AdditionalMetadata, provenance1.AdditionalMetadata, provenance2.AdditionalMetadata);

            return (provenance1, provenance2);
        }

        private void SplitMetadata(Dictionary<string, string> source, Dictionary<string, string> target1, Dictionary<string, string> target2)
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
            return $"ProvenanceId: {ProvenanceId}, Level: {Level}, Agency: {AgencyContext}, Metadata Count: {AdditionalMetadata.Count}";
        }
    }
}