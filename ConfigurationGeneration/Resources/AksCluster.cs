using System;
using ConfigurationGeneration.Topology;

namespace ConfigurationGeneration.Resources
{
    /// <summary>
    /// Azure Kubernetes Service (AKS) cluster resource.
    /// Converts ARM template AKS definitions to type-safe C# configuration.
    /// </summary>
    public class AksCluster : AzureResource
    {
        // Basic properties
        public string ClusterName { get; set; }
        public string DnsPrefix { get; set; }
        
        // Node pool configuration
        public int NodeCount { get; set; }
        public int MinNodeCount { get; set; }
        public int MaxNodeCount { get; set; }
        public string VmSize { get; set; }
        public string OsDiskSizeGB { get; set; }
        public string OsType { get; set; } = "Linux";
        
        // Networking
        public string NetworkPlugin { get; set; } = "azure";
        public string NetworkPolicy { get; set; } = "azure";
        public string ServiceCidr { get; set; }
        public string DnsServiceIP { get; set; }
        public string DockerBridgeCidr { get; set; }
        
        // High availability
        public string[] AvailabilityZones { get; set; }
        public bool EnableAutoScaling { get; set; }
        
        // Security
        public bool EnableRBAC { get; set; } = true;
        public bool EnablePodSecurityPolicy { get; set; }
        public string ServicePrincipalClientId { get; set; }
        public string ServicePrincipalSecret { get; set; }
        
        // Monitoring
        public bool EnableMonitoring { get; set; } = true;
        public string LogAnalyticsWorkspaceId { get; set; }

        public AksCluster(string name, string region, ResourceGroup resourceGroup)
        {
            ClusterName = name;
            Name = name;
            Location = region;
            ResourceGroup = resourceGroup;
            
            DnsPrefix = $"{name}-dns";
            
            // Set defaults
            NodeCount = 3;
            MinNodeCount = 1;
            MaxNodeCount = 10;
            VmSize = "Standard_D4s_v3";
            OsDiskSizeGB = "128";
            
            // Default networking
            ServiceCidr = "10.0.0.0/16";
            DnsServiceIP = "10.0.0.10";
            DockerBridgeCidr = "172.17.0.1/16";
            
            EnableAutoScaling = true;
        }

        /// <summary>
        /// Applies region-specific configurations
        /// </summary>
        public void ApplyRegionConfiguration(Environments.DataCenter dataCenter)
        {
            // Adjust VM size based on region capabilities
            if (dataCenter.Region == "italynorth")
            {
                // Italy North may not support all SKUs
                VmSize = "Standard_D4_v3";
            }
            else if (dataCenter.Cloud == CloudType.Fairfax)
            {
                // Fairfax may have different SKU names
                VmSize = "Standard_D4_v3";
            }
            
            // Configure availability zones
            if (dataCenter.SupportsAvailabilityZones && dataCenter.Zones != null)
            {
                AvailabilityZones = dataCenter.Zones;
            }
            
            // Apply node count limits
            if (dataCenter.MaxNodeCount.HasValue)
            {
                MaxNodeCount = dataCenter.MaxNodeCount.Value;
                if (NodeCount > MaxNodeCount)
                {
                    NodeCount = MaxNodeCount;
                }
            }
        }

        /// <summary>
        /// Generates ARM template JSON for this AKS cluster
        /// </summary>
        public string GenerateArmTemplate()
        {
            return $@"{{
  ""type"": ""Microsoft.ContainerService/managedClusters"",
  ""apiVersion"": ""2021-05-01"",
  ""name"": ""{ClusterName}"",
  ""location"": ""{Location}"",
  ""properties"": {{
    ""kubernetesVersion"": ""1.21.2"",
    ""dnsPrefix"": ""{DnsPrefix}"",
    ""agentPoolProfiles"": [
      {{
        ""name"": ""nodepool1"",
        ""count"": {NodeCount},
        ""vmSize"": ""{VmSize}"",
        ""osDiskSizeGB"": {OsDiskSizeGB},
        ""osType"": ""{OsType}"",
        ""minCount"": {MinNodeCount},
        ""maxCount"": {MaxNodeCount},
        ""enableAutoScaling"": {EnableAutoScaling.ToString().ToLower()},
        {(AvailabilityZones != null && AvailabilityZones.Length > 0 ? $@"""availabilityZones"": [{string.Join(",", Array.ConvertAll(AvailabilityZones, z => $@"""{z}"""))}]," : "")}
        ""type"": ""VirtualMachineScaleSets"",
        ""mode"": ""System""
      }}
    ],
    ""networkProfile"": {{
      ""networkPlugin"": ""{NetworkPlugin}"",
      ""networkPolicy"": ""{NetworkPolicy}"",
      ""serviceCidr"": ""{ServiceCidr}"",
      ""dnsServiceIP"": ""{DnsServiceIP}"",
      ""dockerBridgeCidr"": ""{DockerBridgeCidr}""
    }},
    ""enableRBAC"": {EnableRBAC.ToString().ToLower()},
    ""addonProfiles"": {{
      ""omsagent"": {{
        ""enabled"": {EnableMonitoring.ToString().ToLower()},
        ""config"": {{
          ""logAnalyticsWorkspaceResourceID"": ""{LogAnalyticsWorkspaceId}""
        }}
      }}
    }}
  }},
  ""identity"": {{
    ""type"": ""SystemAssigned""
  }}
}}";
        }
    }
}
