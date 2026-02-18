using System.Collections.Generic;

namespace ConfigurationGeneration.Topology.Environments
{
    /// <summary>
    /// Pre-Production Environment (PPE) configuration.
    /// Used for final validation before production deployment.
    /// Similar to production but with reduced capacity.
    /// </summary>
    public class PPE : EnvironmentBase
    {
        public PPE()
        {
            Name = "PPE";
            IsProduction = false;
            TenantId = "ppe-tenant-id";

            // PPE deploys to multiple regions with production-like setup
            DataCenters = new List<DataCenter>
            {
                new DataCenter
                {
                    Region = "eastus",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2" }, // Multi-zone but not all zones
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = false,
                    MaxNodeCount = 3,
                    MaxPartitions = 8
                },
                new DataCenter
                {
                    Region = "westus2",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = false,
                    MaxNodeCount = 3,
                    MaxPartitions = 8
                },
                new DataCenter
                {
                    Region = "northeurope",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = false,
                    MaxNodeCount = 3,
                    MaxPartitions = 8
                }
            };
        }
    }
}
