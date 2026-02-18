using System.Collections.Generic;

namespace ConfigurationGeneration.Topology.Environments
{
    /// <summary>
    /// Fairfax (Azure Government) environment configuration.
    /// Special sovereign cloud with compliance requirements and limited region availability.
    /// </summary>
    public class Fairfax : EnvironmentBase
    {
        public Fairfax()
        {
            Name = "Fairfax";
            IsProduction = true;
            TenantId = "fairfax-tenant-id";

            // Fairfax has limited regions and different compliance requirements
            DataCenters = new List<DataCenter>
            {
                new DataCenter
                {
                    Region = "usgovvirginia",
                    Cloud = CloudType.Fairfax,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true, // CMK often required for government
                    MaxNodeCount = 8,
                    MaxPartitions = 16,
                    KeyVaultUri = "https://keyvault-usgovvirginia.vault.usgovcloudapi.net/"
                },
                new DataCenter
                {
                    Region = "usgovtexas",
                    Cloud = CloudType.Fairfax,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 8,
                    MaxPartitions = 16,
                    KeyVaultUri = "https://keyvault-usgovtexas.vault.usgovcloudapi.net/"
                },
                new DataCenter
                {
                    Region = "usgovarizona",
                    Cloud = CloudType.Fairfax,
                    SupportsAvailabilityZones = false, // Some gov regions don't have zones
                    Zones = new string[] { },
                    SupportsAks = true,
                    SupportsZoneRedundancy = false,
                    RequiresCMK = true,
                    MaxNodeCount = 6,
                    MaxPartitions = 8,
                    KeyVaultUri = "https://keyvault-usgovarizona.vault.usgovcloudapi.net/"
                }
            };
        }
    }
}
