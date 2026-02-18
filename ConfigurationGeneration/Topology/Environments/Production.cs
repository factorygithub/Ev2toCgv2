using System.Collections.Generic;

namespace ConfigurationGeneration.Topology.Environments
{
    /// <summary>
    /// Production environment configuration.
    /// Full-scale deployment with high availability and disaster recovery.
    /// </summary>
    public class Production : EnvironmentBase
    {
        public Production()
        {
            Name = "Production";
            IsProduction = true;
            TenantId = "prod-tenant-id";

            // Production deploys to all major regions with full capabilities
            DataCenters = new List<DataCenter>
            {
                new DataCenter
                {
                    Region = "eastus",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" }, // All zones for HA
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 10,
                    MaxPartitions = 32,
                    KeyVaultUri = "https://keyvault-eastus.vault.azure.net/"
                },
                new DataCenter
                {
                    Region = "westus2",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 10,
                    MaxPartitions = 32,
                    KeyVaultUri = "https://keyvault-westus2.vault.azure.net/"
                },
                new DataCenter
                {
                    Region = "northeurope",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 10,
                    MaxPartitions = 32,
                    KeyVaultUri = "https://keyvault-northeurope.vault.azure.net/"
                },
                new DataCenter
                {
                    Region = "westeurope",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 10,
                    MaxPartitions = 32,
                    KeyVaultUri = "https://keyvault-westeurope.vault.azure.net/"
                },
                new DataCenter
                {
                    Region = "southeastasia",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2", "3" },
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 10,
                    MaxPartitions = 32,
                    KeyVaultUri = "https://keyvault-southeastasia.vault.azure.net/"
                },
                // Italy North - newer region with limited initial support
                new DataCenter
                {
                    Region = "italynorth",
                    Cloud = CloudType.Public,
                    SupportsAvailabilityZones = true,
                    Zones = new[] { "1", "2" }, // Only 2 zones initially
                    SupportsAks = true,
                    SupportsZoneRedundancy = true,
                    RequiresCMK = true,
                    MaxNodeCount = 8, // Lower quota initially
                    MaxPartitions = 16, // Conservative partition count
                    KeyVaultUri = "https://keyvault-italynorth.vault.azure.net/"
                }
            };
        }
    }
}
