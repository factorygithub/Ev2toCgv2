using System;
using Xunit;
using ConfigurationGeneration.Resources;
using ConfigurationGeneration.Topology;
using ConfigurationGeneration.Topology.Environments;

namespace ConfigurationGeneration.Tests
{
    /// <summary>
    /// Tests for Azure Resource configurations
    /// </summary>
    public class ResourceTests
    {
        [Fact]
        public void StorageAccount_Name_Should_Be_Sanitized()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            
            // Act
            var storage = new StorageAccount("Test-Storage_Account", "eastus", rg);
            
            // Assert
            Assert.DoesNotContain("-", storage.AccountName);
            Assert.DoesNotContain("_", storage.AccountName);
            Assert.True(storage.AccountName.Length <= 24, "Storage account name should be <= 24 characters");
        }

        [Fact]
        public void StorageAccount_Should_Use_GRS_In_Production()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var storage = new StorageAccount("teststorage", "eastus", rg);
            var prodEnv = new Production();
            var dataCenterWithZRS = new DataCenter 
            { 
                Region = "eastus", 
                SupportsZoneRedundancy = true 
            };
            
            // Act
            storage.ApplyConfiguration(prodEnv, dataCenterWithZRS);
            
            // Assert
            Assert.Contains("ZRS", storage.Sku); // Should use ZRS when supported
        }

        [Fact]
        public void StorageAccount_Should_Use_LRS_In_Test()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var storage = new StorageAccount("teststorage", "eastus", rg);
            var testEnv = new Test();
            var dataCenter = new DataCenter { Region = "eastus" };
            
            // Act
            storage.ApplyConfiguration(testEnv, dataCenter);
            
            // Assert
            Assert.Equal("Standard_LRS", storage.Sku);
        }

        [Fact]
        public void AksCluster_Should_Apply_Region_Specific_SKU()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var aks = new AksCluster("test-aks", "italynorth", rg);
            var italyNorth = new DataCenter 
            { 
                Region = "italynorth",
                MaxNodeCount = 8
            };
            
            // Act
            aks.ApplyRegionConfiguration(italyNorth);
            
            // Assert
            Assert.Equal("Standard_D4_v3", aks.VmSize); // Should use v3 for Italy North
            Assert.True(aks.MaxNodeCount <= 8); // Should respect regional limits
        }

        [Fact]
        public void AksCluster_Should_Configure_Zones_When_Supported()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var aks = new AksCluster("test-aks", "eastus", rg);
            var dataCenter = new DataCenter 
            { 
                Region = "eastus",
                SupportsAvailabilityZones = true,
                Zones = new[] { "1", "2", "3" }
            };
            
            // Act
            aks.ApplyRegionConfiguration(dataCenter);
            
            // Assert
            Assert.NotNull(aks.AvailabilityZones);
            Assert.Equal(3, aks.AvailabilityZones.Length);
        }

        [Fact]
        public void EventHub_Should_Use_Higher_Partitions_In_Production()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var eventHub = new EventHub("test-ns", "test-eh", "eastus", rg);
            var prodEnv = new Production();
            var dataCenter = new DataCenter 
            { 
                Region = "eastus",
                MaxPartitions = 32
            };
            
            // Act
            eventHub.ApplyConfiguration(prodEnv, dataCenter);
            
            // Assert
            Assert.Equal(32, eventHub.PartitionCount);
            Assert.Equal("Standard", eventHub.Sku);
            Assert.True(eventHub.EnableAutoInflate);
        }

        [Fact]
        public void EventHub_Should_Use_Basic_SKU_In_Test()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var eventHub = new EventHub("test-ns", "test-eh", "eastus", rg);
            var testEnv = new Test();
            var dataCenter = new DataCenter { Region = "eastus" };
            
            // Act
            eventHub.ApplyConfiguration(testEnv, dataCenter);
            
            // Assert
            Assert.Equal("Basic", eventHub.Sku);
            Assert.Equal(4, eventHub.PartitionCount);
            Assert.False(eventHub.EnableAutoInflate);
        }

        [Fact]
        public void RoleAssignment_Should_Resolve_Well_Known_Roles()
        {
            // Arrange & Act
            var roleAssignment = new RoleAssignment("StorageBlobDataContributor", "test-principal-id", "test-scope");
            
            // Assert
            Assert.NotNull(roleAssignment.RoleDefinitionId);
            Assert.NotEmpty(roleAssignment.RoleDefinitionId);
            Assert.NotEqual("StorageBlobDataContributor", roleAssignment.RoleDefinitionId); // Should be resolved to GUID
        }

        [Fact]
        public void RoleAssignment_Should_Create_For_Storage_Account()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var storage = new StorageAccount("teststorage", "eastus", rg);
            
            // Act
            var roleAssignment = RoleAssignment.ForStorageAccount(
                "StorageBlobDataReader", 
                "test-principal-id", 
                storage
            );
            
            // Assert
            Assert.NotNull(roleAssignment);
            Assert.Contains("storageAccounts", roleAssignment.Scope);
        }

        [Fact]
        public void TableService_Should_Have_Valid_Name()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var storage = new StorageAccount("teststorage", "eastus", rg);
            
            // Act
            var table = new TableService("TestTable", storage);
            
            // Assert
            Assert.Equal("TestTable", table.TableName);
            Assert.Contains(storage.AccountName, table.Name);
        }

        [Fact]
        public void Resources_Should_Generate_Valid_ARM_Templates()
        {
            // Arrange
            var rg = CreateTestResourceGroup();
            var storage = new StorageAccount("teststorage", "eastus", rg);
            
            // Act
            var armTemplate = storage.GenerateArmTemplate();
            
            // Assert
            Assert.NotNull(armTemplate);
            Assert.Contains("Microsoft.Storage/storageAccounts", armTemplate);
            Assert.Contains(storage.AccountName, armTemplate);
            Assert.Contains("TLS1_2", armTemplate);
        }

        // Helper method
        private ResourceGroup CreateTestResourceGroup()
        {
            return new ResourceGroup
            {
                Name = "test-rg",
                Location = "eastus",
                Subscription = new Subscription
                {
                    Id = "test-subscription-id",
                    Name = "test-subscription"
                }
            };
        }
    }
}
