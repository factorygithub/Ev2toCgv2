using ConfigurationGeneration.Topology;

namespace ConfigurationGeneration.Resources
{
    /// <summary>
    /// Azure Event Hub resource.
    /// Converts ARM template Event Hub definitions to type-safe C# configuration.
    /// </summary>
    public class EventHub : AzureResource
    {
        // Namespace properties
        public string NamespaceName { get; set; }
        public string EventHubName { get; set; }
        public string Sku { get; set; }
        public int Capacity { get; set; }
        
        // Event Hub properties
        public int PartitionCount { get; set; }
        public int MessageRetentionInDays { get; set; }
        
        // Security
        public bool EnableAutoInflate { get; set; }
        public int MaxThroughputUnits { get; set; }
        public bool RequireInfrastructureEncryption { get; set; }
        
        // Zone redundancy
        public bool ZoneRedundant { get; set; }
        
        // Capture configuration
        public bool EnableCapture { get; set; }
        public string CaptureStorageAccountId { get; set; }
        public string CaptureBlobContainer { get; set; }
        public int CaptureIntervalInSeconds { get; set; } = 300;
        public int CaptureSizeLimitInBytes { get; set; } = 314572800; // 300 MB

        public EventHub(string namespaceName, string eventHubName, string region, ResourceGroup resourceGroup)
        {
            NamespaceName = namespaceName;
            EventHubName = eventHubName;
            Name = $"{namespaceName}/{eventHubName}";
            Location = region;
            ResourceGroup = resourceGroup;
            
            // Default configuration
            Sku = "Standard";
            Capacity = 1;
            PartitionCount = 4;
            MessageRetentionInDays = 1;
            
            EnableAutoInflate = false;
            MaxThroughputUnits = 20;
        }

        /// <summary>
        /// Applies environment and region-specific configurations
        /// </summary>
        public void ApplyConfiguration(Environments.EnvironmentBase environment, Environments.DataCenter dataCenter)
        {
            // Production gets higher tier and more partitions
            if (environment.IsProduction)
            {
                Sku = "Standard";
                MessageRetentionInDays = 7;
                EnableAutoInflate = true;
                
                // Apply partition limits
                if (dataCenter.MaxPartitions.HasValue)
                {
                    PartitionCount = dataCenter.MaxPartitions.Value;
                }
                else
                {
                    PartitionCount = 32;
                }
            }
            else
            {
                Sku = "Basic";
                PartitionCount = 4;
                MessageRetentionInDays = 1;
            }
            
            // Enable zone redundancy if supported
            if (dataCenter.SupportsZoneRedundancy)
            {
                ZoneRedundant = true;
            }
            
            // Enable infrastructure encryption for compliance
            if (dataCenter.RequiresCMK)
            {
                RequireInfrastructureEncryption = true;
            }
        }

        /// <summary>
        /// Configures Event Hub Capture to storage
        /// </summary>
        public void ConfigureCapture(string storageAccountId, string containerName)
        {
            EnableCapture = true;
            CaptureStorageAccountId = storageAccountId;
            CaptureBlobContainer = containerName;
        }

        /// <summary>
        /// Generates ARM template JSON for this Event Hub
        /// </summary>
        public string GenerateArmTemplate()
        {
            // Namespace
            var namespaceTemplate = $@"{{
  ""type"": ""Microsoft.EventHub/namespaces"",
  ""apiVersion"": ""2021-06-01-preview"",
  ""name"": ""{NamespaceName}"",
  ""location"": ""{Location}"",
  ""sku"": {{
    ""name"": ""{Sku}"",
    ""tier"": ""{Sku}"",
    ""capacity"": {Capacity}
  }},
  ""properties"": {{
    ""isAutoInflateEnabled"": {EnableAutoInflate.ToString().ToLower()},
    ""maximumThroughputUnits"": {MaxThroughputUnits},
    ""zoneRedundant"": {ZoneRedundant.ToString().ToLower()}
  }}
}}";

            var captureBlock = EnableCapture
                ? $@"""captureDescription"": {{
      ""enabled"": true,
      ""encoding"": ""Avro"",
      ""intervalInSeconds"": {CaptureIntervalInSeconds},
      ""sizeLimitInBytes"": {CaptureSizeLimitInBytes},
      ""destination"": {{
        ""name"": ""EventHubArchive.AzureBlockBlob"",
        ""properties"": {{
          ""storageAccountResourceId"": ""{CaptureStorageAccountId}"",
          ""blobContainer"": ""{CaptureBlobContainer}""
        }}
      }}
    }},"
                : "";

            // Event Hub
            var eventHubTemplate = $@"{{
  ""type"": ""Microsoft.EventHub/namespaces/eventhubs"",
  ""apiVersion"": ""2021-06-01-preview"",
  ""name"": ""{Name}"",
  ""dependsOn"": [
    ""[resourceId('Microsoft.EventHub/namespaces', '{NamespaceName}')]""
  ],
  ""properties"": {{
    ""messageRetentionInDays"": {MessageRetentionInDays},
    ""partitionCount"": {PartitionCount},
    {captureBlock}
  }}
}}";

            return $"{namespaceTemplate},\n{eventHubTemplate}";
        }
    }
}
