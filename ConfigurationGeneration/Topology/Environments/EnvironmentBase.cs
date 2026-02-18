using System.Collections.Generic;

namespace ConfigurationGeneration.Topology.Environments
{
    /// <summary>
    /// Base class for environment-specific configurations.
    /// </summary>
    public abstract class EnvironmentBase
    {
        public string Name { get; set; }
        public bool IsProduction { get; set; }
        public List<DataCenter> DataCenters { get; set; }
        public string TenantId { get; set; }

        protected EnvironmentBase()
        {
            DataCenters = new List<DataCenter>();
        }
    }

    /// <summary>
    /// Represents a datacenter (region) with its capabilities and constraints.
    /// </summary>
    public class DataCenter
    {
        public string Region { get; set; }
        public CloudType Cloud { get; set; } = CloudType.Public;
        public bool SupportsAvailabilityZones { get; set; }
        public string[] Zones { get; set; }
        public bool SupportsAks { get; set; } = true;
        public bool SupportsZoneRedundancy { get; set; }
        public bool RequiresCMK { get; set; }
        public int? MaxNodeCount { get; set; }
        public int? MaxPartitions { get; set; }
        public string KeyVaultUri { get; set; }
    }
}
