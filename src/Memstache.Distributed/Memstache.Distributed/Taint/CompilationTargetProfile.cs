using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MemStache.Distributed.TaintStash
{
    public class CompilationTargetProfile
    {
        public string ProfileId { get; private set; }
        public Dictionary<string, string> HardwareRequirements { get; private set; }
        public Dictionary<string, string> SoftwareRequirements { get; private set; }
        public Dictionary<string, string> EnvironmentalConstraints { get; private set; }

        public CompilationTargetProfile()
        {
            ProfileId = Guid.NewGuid().ToString();
            HardwareRequirements = new Dictionary<string, string>();
            SoftwareRequirements = new Dictionary<string, string>();
            EnvironmentalConstraints = new Dictionary<string, string>();
        }

        public void AddHardwareRequirement(string key, string value)
        {
            HardwareRequirements[key] = value;
        }

        public void AddSoftwareRequirement(string key, string value)
        {
            SoftwareRequirements[key] = value;
        }

        public void AddEnvironmentalConstraint(string key, string value)
        {
            EnvironmentalConstraints[key] = value;
        }

        public bool Verify()
        {
            return VerifyHardwareRequirements() &&
                   VerifySoftwareRequirements() &&
                   VerifyEnvironmentalConstraints();
        }

        private bool VerifyHardwareRequirements()
        {
            foreach (var requirement in HardwareRequirements)
            {
                switch (requirement.Key)
                {
                    case "Architecture":
                        if (requirement.Value != RuntimeInformation.ProcessArchitecture.ToString())
                            return false;
                        break;
                    case "ProcessorCount":
                        if (int.Parse(requirement.Value) > Environment.ProcessorCount)
                            return false;
                        break;
                    // Add more hardware checks as needed
                    default:
                        // Log unknown requirement
                        Console.WriteLine($"Unknown hardware requirement: {requirement.Key}");
                        break;
                }
            }
            return true;
        }

        private bool VerifySoftwareRequirements()
        {
            foreach (var requirement in SoftwareRequirements)
            {
                switch (requirement.Key)
                {
                    case "OSPlatform":
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create(requirement.Value)))
                            return false;
                        break;
                    case "FrameworkVersion":
                        if (Environment.Version < Version.Parse(requirement.Value))
                            return false;
                        break;
                    // Add more software checks as needed
                    default:
                        // Log unknown requirement
                        Console.WriteLine($"Unknown software requirement: {requirement.Key}");
                        break;
                }
            }
            return true;
        }

        private bool VerifyEnvironmentalConstraints()
        {
            foreach (var constraint in EnvironmentalConstraints)
            {
                switch (constraint.Key)
                {
                    case "TimeZone":
                        if (TimeZoneInfo.Local.Id != constraint.Value)
                            return false;
                        break;
                    case "EnvironmentVariable":
                        var parts = constraint.Value.Split('=');
                        if (parts.Length != 2 || Environment.GetEnvironmentVariable(parts[0]) != parts[1])
                            return false;
                        break;
                    // Add more environmental checks as needed
                    default:
                        // Log unknown constraint
                        Console.WriteLine($"Unknown environmental constraint: {constraint.Key}");
                        break;
                }
            }
            return true;
        }

        public static CompilationTargetProfile GenerateFromCurrentEnvironment()
        {
            var profile = new CompilationTargetProfile();

            // Hardware requirements
            profile.AddHardwareRequirement("Architecture", RuntimeInformation.ProcessArchitecture.ToString());
            profile.AddHardwareRequirement("ProcessorCount", Environment.ProcessorCount.ToString());

            // Software requirements
            profile.AddSoftwareRequirement("OSPlatform", GetOSPlatform());
            profile.AddSoftwareRequirement("FrameworkVersion", Environment.Version.ToString());

            // Environmental constraints
            profile.AddEnvironmentalConstraint("TimeZone", TimeZoneInfo.Local.Id);

            return profile;
        }

        private static string GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "OSX";
            return "Unknown";
        }

        public static CompilationTargetProfile Combine(CompilationTargetProfile profile1, CompilationTargetProfile profile2)
        {
            var combinedProfile = new CompilationTargetProfile();

            CombineDictionaries(combinedProfile.HardwareRequirements, profile1.HardwareRequirements, profile2.HardwareRequirements);
            CombineDictionaries(combinedProfile.SoftwareRequirements, profile1.SoftwareRequirements, profile2.SoftwareRequirements);
            CombineDictionaries(combinedProfile.EnvironmentalConstraints, profile1.EnvironmentalConstraints, profile2.EnvironmentalConstraints);

            return combinedProfile;
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
                    // If the key already exists, take the more restrictive value
                    target[kvp.Key] = CompareValues(target[kvp.Key], kvp.Value);
                }
            }
        }

        private static string CompareValues(string value1, string value2)
        {
            // This is a simplified comparison. You might need to implement more sophisticated logic
            // depending on the nature of your requirements and constraints.
            if (int.TryParse(value1, out int intValue1) && int.TryParse(value2, out int intValue2))
            {
                return Math.Max(intValue1, intValue2).ToString();
            }
            return value1.CompareTo(value2) > 0 ? value1 : value2;
        }

        public (CompilationTargetProfile, CompilationTargetProfile) Split()
        {
            var profile1 = new CompilationTargetProfile();
            var profile2 = new CompilationTargetProfile();

            SplitDictionary(this.HardwareRequirements, profile1.HardwareRequirements, profile2.HardwareRequirements);
            SplitDictionary(this.SoftwareRequirements, profile1.SoftwareRequirements, profile2.SoftwareRequirements);
            SplitDictionary(this.EnvironmentalConstraints, profile1.EnvironmentalConstraints, profile2.EnvironmentalConstraints);

            return (profile1, profile2);
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
    }
}
