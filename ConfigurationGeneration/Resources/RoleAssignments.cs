using System.Collections.Generic;
using ConfigurationGeneration.Topology;

namespace ConfigurationGeneration.Resources
{
    /// <summary>
    /// Azure Role Assignment resource for RBAC.
    /// Converts ARM template role assignment definitions to type-safe C# configuration.
    /// </summary>
    public class RoleAssignment : AzureResource
    {
        // Role assignment properties
        public string RoleDefinitionId { get; set; }
        public string PrincipalId { get; set; }
        public string PrincipalType { get; set; }
        public string Scope { get; set; }
        
        // Role definition helpers
        public static readonly Dictionary<string, string> WellKnownRoles = new Dictionary<string, string>
        {
            { "Owner", "8e3af657-a8ff-443c-a75c-2fe8c4bcb635" },
            { "Contributor", "b24988ac-6180-42a0-ab88-20f7382dd24c" },
            { "Reader", "acdd72a7-3385-48ef-bd42-f606fba81ae7" },
            { "StorageBlobDataContributor", "ba92f5b4-2d11-453d-a403-e96b0029c9fe" },
            { "StorageBlobDataReader", "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1" },
            { "StorageTableDataContributor", "0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3" },
            { "StorageTableDataReader", "76199698-9eea-4c19-bc75-cec21354c6b6" },
            { "EventHubsDataOwner", "f526a384-b230-433a-b45c-95f59c4a2dec" },
            { "EventHubsDataSender", "2b629674-e913-4c01-ae53-ef4638d8f975" },
            { "EventHubsDataReceiver", "a638d3c7-ab3a-418d-83e6-5f17a39d4fde" },
            { "AKSClusterAdmin", "0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8" },
            { "AKSClusterUser", "4abbcc35-e782-43d8-92c5-2d3f1bd2253f" },
            { "KeyVaultAdministrator", "00482a5a-887f-4fb3-b363-3b7fe8e74483" },
            { "KeyVaultSecretsUser", "4633458b-17de-408a-b874-0445c86b69e6" }
        };

        public RoleAssignment(string roleName, string principalId, string scope)
        {
            Name = System.Guid.NewGuid().ToString();
            
            // Resolve role definition ID
            if (WellKnownRoles.ContainsKey(roleName))
            {
                RoleDefinitionId = WellKnownRoles[roleName];
            }
            else
            {
                RoleDefinitionId = roleName; // Assume it's already a GUID
            }
            
            PrincipalId = principalId;
            PrincipalType = "ServicePrincipal"; // Default
            Scope = scope;
        }

        /// <summary>
        /// Creates a role assignment for a storage account
        /// </summary>
        public static RoleAssignment ForStorageAccount(string roleName, string principalId, StorageAccount storageAccount)
        {
            var scope = $"/subscriptions/{storageAccount.ResourceGroup.Subscription.Id}/resourceGroups/{storageAccount.ResourceGroup.Name}/providers/Microsoft.Storage/storageAccounts/{storageAccount.AccountName}";
            return new RoleAssignment(roleName, principalId, scope);
        }

        /// <summary>
        /// Creates a role assignment for an Event Hub namespace
        /// </summary>
        public static RoleAssignment ForEventHub(string roleName, string principalId, EventHub eventHub)
        {
            var scope = $"/subscriptions/{eventHub.ResourceGroup.Subscription.Id}/resourceGroups/{eventHub.ResourceGroup.Name}/providers/Microsoft.EventHub/namespaces/{eventHub.NamespaceName}";
            return new RoleAssignment(roleName, principalId, scope);
        }

        /// <summary>
        /// Creates a role assignment for an AKS cluster
        /// </summary>
        public static RoleAssignment ForAksCluster(string roleName, string principalId, AksCluster aksCluster)
        {
            var scope = $"/subscriptions/{aksCluster.ResourceGroup.Subscription.Id}/resourceGroups/{aksCluster.ResourceGroup.Name}/providers/Microsoft.ContainerService/managedClusters/{aksCluster.ClusterName}";
            return new RoleAssignment(roleName, principalId, scope);
        }

        /// <summary>
        /// Generates ARM template JSON for this role assignment
        /// </summary>
        public string GenerateArmTemplate()
        {
            return $@"{{
  ""type"": ""Microsoft.Authorization/roleAssignments"",
  ""apiVersion"": ""2020-04-01-preview"",
  ""name"": ""{Name}"",
  ""properties"": {{
    ""roleDefinitionId"": ""[concat(subscription().id, '/providers/Microsoft.Authorization/roleDefinitions/', '{RoleDefinitionId}')]"",
    ""principalId"": ""{PrincipalId}"",
    ""principalType"": ""{PrincipalType}"",
    ""scope"": ""{Scope}""
  }}
}}";
        }
    }
}
