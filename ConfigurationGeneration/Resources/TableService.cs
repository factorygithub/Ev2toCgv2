using ConfigurationGeneration.Topology;

namespace ConfigurationGeneration.Resources
{
    /// <summary>
    /// Azure Table Service resource (part of Storage Account).
    /// Converts ARM template table definitions to type-safe C# configuration.
    /// </summary>
    public class TableService : AzureResource
    {
        // Parent storage account
        public StorageAccount StorageAccount { get; set; }
        
        // Table properties
        public string TableName { get; set; }
        
        // CORS configuration
        public bool EnableCors { get; set; }
        public string[] AllowedOrigins { get; set; }
        public string[] AllowedMethods { get; set; }
        public string[] AllowedHeaders { get; set; }
        public string[] ExposedHeaders { get; set; }
        public int MaxAgeInSeconds { get; set; } = 3600;

        public TableService(string tableName, StorageAccount storageAccount)
        {
            TableName = tableName;
            Name = $"{storageAccount.AccountName}/default/{tableName}";
            StorageAccount = storageAccount;
            Location = storageAccount.Location;
            ResourceGroup = storageAccount.ResourceGroup;
            
            // Default CORS configuration
            EnableCors = false;
        }

        /// <summary>
        /// Configures CORS for the table
        /// </summary>
        public void ConfigureCors(string[] origins, string[] methods)
        {
            EnableCors = true;
            AllowedOrigins = origins;
            AllowedMethods = methods;
            AllowedHeaders = new[] { "*" };
            ExposedHeaders = new[] { "*" };
        }

        /// <summary>
        /// Generates ARM template JSON for this table service
        /// </summary>
        public string GenerateArmTemplate()
        {
            var corsBlock = EnableCors && AllowedOrigins != null
                ? $@"""cors"": {{
        ""corsRules"": [
          {{
            ""allowedOrigins"": [{string.Join(",", AllowedOrigins)}],
            ""allowedMethods"": [{string.Join(",", AllowedMethods)}],
            ""allowedHeaders"": [{string.Join(",", AllowedHeaders)}],
            ""exposedHeaders"": [{string.Join(",", ExposedHeaders)}],
            ""maxAgeInSeconds"": {MaxAgeInSeconds}
          }}
        ]
      }},"
                : "";

            return $@"{{
  ""type"": ""Microsoft.Storage/storageAccounts/tableServices/tables"",
  ""apiVersion"": ""2021-04-01"",
  ""name"": ""{Name}"",
  ""dependsOn"": [
    ""[resourceId('Microsoft.Storage/storageAccounts', '{StorageAccount.AccountName}')]""
  ],
  ""properties"": {{
    {corsBlock}
  }}
}}";
        }
    }
}
