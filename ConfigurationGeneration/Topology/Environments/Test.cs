using System.Collections.Generic;

namespace ConfigurationGeneration.Topology.Environments
{
    /// <summary>
    /// Test environment configuration.
    /// Used for development and early testing with minimal resources.
    /// </summary>
    public class Test : EnvironmentBase
    {
        public Test()
        {
            Name = "Test";
            IsProduction = false;
            TenantId = "test-tenant-id";

            // Test deploys to limited regions with basic SKUs
            DataCenters = new List<DataCenter>
            {
                new DataCenter
                {
                    Region = "eastus",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1" }, // Single zone for cost savings
                    SupportsAks = true,
                    SupportsZoneRedundancy = false,
                    RequiresCMK = false,
                    MaxNodeCount = 2,
                    MaxPartitions = 4
                },
                new DataCenter
                {
                    Region = "westus2",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = false,
                    RequiresCMK = false,
                    MaxNodeCount = 2,
                    MaxPartitions = 4
                }
            };
        }
    }
}
