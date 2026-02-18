using System;
using Xunit;
using ConfigurationGeneration.Topology;
using ConfigurationGeneration.Topology.Environments;

namespace ConfigurationGeneration.Tests
{
    /// <summary>
    /// Tests for Topology validation
    /// </summary>
    public class TopologyTests
    {
        [Fact]
        public void Test_Environment_Should_Have_DataCenters()
        {
            // Arrange
            var testEnv = new Test();
            
            // Assert
            Assert.NotNull(testEnv.DataCenters);
            Assert.NotEmpty(testEnv.DataCenters);
            Assert.Equal("Test", testEnv.Name);
            Assert.False(testEnv.IsProduction);
        }

        [Fact]
        public void PPE_Environment_Should_Have_Multiple_Regions()
        {
            // Arrange
            var ppeEnv = new PPE();
            
            // Assert
            Assert.NotNull(ppeEnv.DataCenters);
            Assert.True(ppeEnv.DataCenters.Count >= 2, "PPE should have multiple regions");
            Assert.Equal("PPE", ppeEnv.Name);
            Assert.False(ppeEnv.IsProduction);
        }

        [Fact]
        public void Production_Environment_Should_Be_Marked_As_Production()
        {
            // Arrange
            var prodEnv = new Production();
            
            // Assert
            Assert.NotNull(prodEnv.DataCenters);
            Assert.True(prodEnv.IsProduction, "Production environment should be marked as production");
            Assert.Equal("Production", prodEnv.Name);
        }

        [Fact]
        public void Production_Should_Include_Italy_North()
        {
            // Arrange
            var prodEnv = new Production();
            
            // Act
            var italyNorth = prodEnv.DataCenters.Find(dc => dc.Region == "italynorth");
            
            // Assert
            Assert.NotNull(italyNorth);
            Assert.True(italyNorth.SupportsAvailabilityZones);
            Assert.Equal(2, italyNorth.Zones.Length); // Italy North has 2 zones
        }

        [Fact]
        public void Fairfax_Environment_Should_Have_Government_Regions()
        {
            // Arrange
            var fairfaxEnv = new Fairfax();
            
            // Assert
            Assert.NotNull(fairfaxEnv.DataCenters);
            Assert.True(fairfaxEnv.DataCenters.Count > 0);
            Assert.Equal("Fairfax", fairfaxEnv.Name);
            Assert.True(fairfaxEnv.IsProduction);
            
            // All Fairfax datacenters should require CMK
            foreach (var dc in fairfaxEnv.DataCenters)
            {
                Assert.Equal(CloudType.Fairfax, dc.Cloud);
                Assert.True(dc.RequiresCMK, $"Fairfax datacenter {dc.Region} should require CMK");
            }
        }

        [Fact]
        public void Production_DataCenters_Should_Have_KeyVault_When_CMK_Required()
        {
            // Arrange
            var prodEnv = new Production();
            
            // Act & Assert
            foreach (var dc in prodEnv.DataCenters)
            {
                if (dc.RequiresCMK)
                {
                    Assert.False(string.IsNullOrEmpty(dc.KeyVaultUri), 
                        $"DataCenter {dc.Region} requires CMK but has no KeyVault URI");
                }
            }
        }

        [Fact]
        public void DataCenter_Zones_Should_Be_Valid()
        {
            // Arrange
            var prodEnv = new Production();
            
            // Act & Assert
            foreach (var dc in prodEnv.DataCenters)
            {
                if (dc.SupportsAvailabilityZones)
                {
                    Assert.NotNull(dc.Zones);
                    Assert.NotEmpty(dc.Zones);
                    Assert.All(dc.Zones, zone => 
                        Assert.Matches(@"^[1-3]$", zone)); // Zones should be 1, 2, or 3
                }
            }
        }

        [Fact]
        public void Test_Environment_Should_Use_Lower_Capacity()
        {
            // Arrange
            var testEnv = new Test();
            var prodEnv = new Production();
            
            // Assert - Test should have lower capacity than Production
            foreach (var testDc in testEnv.DataCenters)
            {
                var prodDc = prodEnv.DataCenters.Find(dc => dc.Region == testDc.Region);
                if (prodDc != null)
                {
                    if (testDc.MaxNodeCount.HasValue && prodDc.MaxNodeCount.HasValue)
                    {
                        Assert.True(testDc.MaxNodeCount.Value <= prodDc.MaxNodeCount.Value,
                            $"Test environment should have lower or equal node count than Production for {testDc.Region}");
                    }
                }
            }
        }

        [Theory]
        [InlineData("eastus")]
        [InlineData("westus2")]
        [InlineData("northeurope")]
        public void Production_Should_Include_Major_Regions(string expectedRegion)
        {
            // Arrange
            var prodEnv = new Production();
            
            // Act
            var region = prodEnv.DataCenters.Find(dc => dc.Region == expectedRegion);
            
            // Assert
            Assert.NotNull(region);
            Assert.Equal(expectedRegion, region.Region);
        }
    }
}
