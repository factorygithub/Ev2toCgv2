using System;
using System.Collections.Generic;

namespace ConfigurationGeneration.Topology
{
    /// <summary>
    /// Main topology definition for the service.
    /// This replaces the EV2 ServiceModel.json with a type-safe, region-agnostic definition.
    /// </summary>
    public class Topology : TopologyBase
    {
        public string ServiceName { get; set; }
        public List<ServiceComponent> Components { get; set; }

        public Topology()
        {
            ServiceName = "MyAzureService";
            Components = new List<ServiceComponent>();

            InitializeTopology();
        }

        private void InitializeTopology()
        {
            // Resolve subscription for the service
            var subscription = Subscription.Resolve($"{ServiceName}-{Environment.Name}");

            // Create resources for each datacenter in the environment
            foreach (var dataCenter in Environment.DataCenters)
            {
                // Create resource group
                var resourceGroup = new ResourceGroup
                {
                    Name = $"{ServiceName}-{dataCenter.Region}-rg",
                    Location = dataCenter.Region,
                    Subscription = subscription
                };

                // Add service components
                Components.Add(new ServiceComponent
                {
                    Name = $"{ServiceName}-{dataCenter.Region}",
                    ResourceGroup = resourceGroup,
                    DataCenter = dataCenter,
                    Resources = CreateResources(dataCenter, resourceGroup)
                });
            }
        }

        private List<AzureResource> CreateResources(DataCenter dataCenter, ResourceGroup resourceGroup)
        {
            var resources = new List<AzureResource>();

            // Add storage account
            resources.Add(new StorageAccountResource
            {
                Name = $"{ServiceName}{dataCenter.Region}sa",
                ResourceGroup = resourceGroup,
                Location = dataCenter.Region,
                Sku = GetStorageSku(dataCenter),
                Kind = "StorageV2"
            });

            // Add AKS cluster if supported in the region
            if (dataCenter.SupportsAks)
            {
                resources.Add(new AksClusterResource
                {
                    Name = $"{ServiceName}-{dataCenter.Region}-aks",
                    ResourceGroup = resourceGroup,
                    Location = dataCenter.Region,
                    NodeCount = GetAksNodeCount(dataCenter),
                    VmSize = GetAksVmSize(dataCenter),
                    Zones = dataCenter.Zones
                });
            }

            // Add Event Hub
            resources.Add(new EventHubResource
            {
                Name = $"{ServiceName}-{dataCenter.Region}-eh",
                ResourceGroup = resourceGroup,
                Location = dataCenter.Region,
                Sku = GetEventHubSku(dataCenter),
                PartitionCount = GetEventHubPartitions(dataCenter)
            });

            return resources;
        }

        private string GetStorageSku(DataCenter dataCenter)
        {
            // Environment-specific SKU selection
            if (Environment.IsProduction)
            {
                return dataCenter.SupportsZoneRedundancy ? "Standard_ZRS" : "Standard_GRS";
            }
            return "Standard_LRS";
        }

        private int GetAksNodeCount(DataCenter dataCenter)
        {
            // Scale based on environment
            if (Environment.IsProduction)
                return dataCenter.MaxNodeCount ?? 5;
            if (Environment.Name == "PPE")
                return 3;
            return 2; // Test
        }

        private string GetAksVmSize(DataCenter dataCenter)
        {
            // Region-specific VM SKU selection
            if (dataCenter.Region == "italynorth")
            {
                // Italy North may have limited SKU availability
                return "Standard_D4_v3";
            }
            if (dataCenter.Cloud == CloudType.Fairfax)
            {
                // Fairfax may have different SKU names
                return "Standard_D4_v3";
            }
            return "Standard_D4s_v3"; // Default with SSD
        }

        private string GetEventHubSku(DataCenter dataCenter)
        {
            return Environment.IsProduction ? "Standard" : "Basic";
        }

        private int GetEventHubPartitions(DataCenter dataCenter)
        {
            // Production gets more partitions for throughput
            if (Environment.IsProduction)
                return dataCenter.MaxPartitions ?? 32;
            return 4;
        }
    }

    // Supporting classes
    public abstract class TopologyBase
    {
        public EnvironmentBase Environment { get; set; }
    }

    public class ServiceComponent
    {
        public string Name { get; set; }
        public ResourceGroup ResourceGroup { get; set; }
        public DataCenter DataCenter { get; set; }
        public List<AzureResource> Resources { get; set; }
    }

    public class ResourceGroup
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public Subscription Subscription { get; set; }
    }

    public class Subscription
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public static Subscription Resolve(string name)
        {
            // In real implementation, this would resolve from a subscription registry
            return new Subscription
            {
                Name = name,
                Id = $"sub-{Guid.NewGuid()}"
            };
        }
    }

    public abstract class AzureResource
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public ResourceGroup ResourceGroup { get; set; }
    }

    public class StorageAccountResource : AzureResource
    {
        public string Sku { get; set; }
        public string Kind { get; set; }
    }

    public class AksClusterResource : AzureResource
    {
        public int NodeCount { get; set; }
        public string VmSize { get; set; }
        public string[] Zones { get; set; }
    }

    public class EventHubResource : AzureResource
    {
        public string Sku { get; set; }
        public int PartitionCount { get; set; }
    }

    public enum CloudType
    {
        Public,
        Fairfax,
        Mooncake
    }
}
