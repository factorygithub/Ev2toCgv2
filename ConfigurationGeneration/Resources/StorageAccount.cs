using System;
using ConfigurationGeneration.Topology;

namespace ConfigurationGeneration.Resources
{
    /// <summary>
    /// Azure Storage Account resource.
    /// Converts ARM template storage definitions to type-safe C# configuration.
    /// </summary>
    public class StorageAccount : AzureResource
    {
        // Basic properties
        public string AccountName { get; set; }
        public string Sku { get; set; }
        public string Kind { get; set; }
        public string AccessTier { get; set; }
        
        // Security
        public bool EnableHttpsTrafficOnly { get; set; } = true;
        public string MinimumTlsVersion { get; set; } = "TLS1_2";
        public bool AllowBlobPublicAccess { get; set; } = false;
        
        // Encryption
        public bool EnableEncryption { get; set; } = true;
        public string KeySource { get; set; } = "Microsoft.Storage";
        public string KeyVaultUri { get; set; }
        public string KeyName { get; set; }
        
        // Networking
        public string DefaultAction { get; set; } = "Deny";
        public string[] AllowedIpRules { get; set; }
        public string[] VirtualNetworkRules { get; set; }
        
        // Features
        public bool EnableBlobVersioning { get; set; }
        public bool EnableBlobSoftDelete { get; set; }
        public int BlobSoftDeleteRetentionDays { get; set; } = 7;

        public StorageAccount(string name, string region, ResourceGroup resourceGroup)
        {
            AccountName = SanitizeStorageAccountName(name);
            Name = AccountName;
            Location = region;
            ResourceGroup = resourceGroup;
            
            // Default configuration
            Kind = "StorageV2";
            Sku = "Standard_LRS";
            AccessTier = "Hot";
            
            EnableBlobVersioning = true;
            EnableBlobSoftDelete = true;
        }

        /// <summary>
        /// Sanitizes storage account name to meet Azure requirements
        /// (lowercase, alphanumeric, 3-24 characters)
        /// </summary>
        private string SanitizeStorageAccountName(string name)
        {
            var sanitized = name.ToLower()
                .Replace("-", "")
                .Replace("_", "");
            
            if (sanitized.Length > 24)
                sanitized = sanitized.Substring(0, 24);
            
            return sanitized;
        }

        /// <summary>
        /// Applies environment and region-specific configurations
        /// </summary>
        public void ApplyConfiguration(Environments.EnvironmentBase environment, Environments.DataCenter dataCenter)
        {
            // Production gets geo-redundancy
            if (environment.IsProduction)
            {
                Sku = dataCenter.SupportsZoneRedundancy ? "Standard_ZRS" : "Standard_GRS";
            }
            else
            {
                Sku = "Standard_LRS"; // Cost savings for non-prod
            }
            
            // Apply customer-managed key if required
            if (dataCenter.RequiresCMK && !string.IsNullOrEmpty(dataCenter.KeyVaultUri))
            {
                KeySource = "Microsoft.Keyvault";
                KeyVaultUri = dataCenter.KeyVaultUri;
                KeyName = $"{AccountName}-key";
            }
            
            // Production enables all security features
            if (environment.IsProduction)
            {
                EnableBlobVersioning = true;
                EnableBlobSoftDelete = true;
                BlobSoftDeleteRetentionDays = 30; // Longer retention in prod
            }
        }

        /// <summary>
        /// Generates ARM template JSON for this storage account
        /// </summary>
        public string GenerateArmTemplate()
        {
            var encryptionBlock = KeySource == "Microsoft.Keyvault" 
                ? $@"""encryption"": {{
      ""services"": {{
        ""blob"": {{ ""enabled"": true }},
        ""file"": {{ ""enabled"": true }}
      }},
      ""keySource"": ""Microsoft.Keyvault"",
      ""keyvaultproperties"": {{
        ""keyname"": ""{KeyName}"",
        ""keyvaulturi"": ""{KeyVaultUri}""
      }}
    }},"
                : $@"""encryption"": {{
      ""services"": {{
        ""blob"": {{ ""enabled"": true }},
        ""file"": {{ ""enabled"": true }}
      }},
      ""keySource"": ""Microsoft.Storage""
    }},";

            return $@"{{
  ""type"": ""Microsoft.Storage/storageAccounts"",
  ""apiVersion"": ""2021-04-01"",
  ""name"": ""{AccountName}"",
  ""location"": ""{Location}"",
  ""sku"": {{
    ""name"": ""{Sku}""
  }},
  ""kind"": ""{Kind}"",
  ""properties"": {{
    ""accessTier"": ""{AccessTier}"",
    ""supportsHttpsTrafficOnly"": {EnableHttpsTrafficOnly.ToString().ToLower()},
    ""minimumTlsVersion"": ""{MinimumTlsVersion}"",
    ""allowBlobPublicAccess"": {AllowBlobPublicAccess.ToString().ToLower()},
    {encryptionBlock}
    ""networkAcls"": {{
      ""defaultAction"": ""{DefaultAction}"",
      ""bypass"": ""AzureServices""
    }}
  }}
}}";
        }
    }
}
