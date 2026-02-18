# EV2 to ConfigGen v2 Migration Guide

## Overview

This guide provides a comprehensive step-by-step process for migrating from manually authored EV2 artifacts to ConfigGen v2 (CGv2) as the source of truth for deployment configurations.

## Table of Contents

1. [Understanding the Responsibility Split](#1-understanding-the-responsibility-split)
2. [Baseline Your Existing EV2 Service](#2-baseline-your-existing-ev2-service)
3. [Create ConfigGen Project Structure](#3-create-configgen-project-structure)
4. [Convert EV2 ServiceModel to ConfigGen Topology](#4-convert-ev2-servicemodel-to-configgen-topology)
5. [Convert ARM Templates to ConfigGen Resources](#5-convert-arm-templates-to-configgen-resources)
6. [Handle Region Differences](#6-handle-region-differences)
7. [EV2 RolloutSpec Generation](#7-ev2-rolloutspec-generation)
8. [EV2 Extensions and Scripts](#8-ev2-extensions-and-scripts)
9. [Pipeline Changes](#9-pipeline-changes)
10. [Validation and Rollout](#10-validation-and-rollout)

---

## 1. Understanding the Responsibility Split

### What Does NOT Change
- **EV2 stays as the deployment orchestrator** for:
  - Rings management
  - Approvals workflow
  - Rollout orchestration
  - EV2 portal functionality
- **Existing ADO EV2 pipelines** and service registration remain intact

### What Changes
- **EV2 artifacts are no longer hand-authored**:
  - ServiceModel.json
  - RolloutSpec.json
  - Parameter files
- **ConfigGen becomes the source of truth** and generates EV2 Region Agnostic (RA) artifacts automatically

### Key Principle
> **ConfigGen authors intent, EV2 executes rollout.**

This is explicitly how EV2 is intended to be used with ConfigGen going forward.

---

## 2. Baseline Your Existing EV2 Service

### Inventory Checklist

From your current `ServiceGroupRoot`, collect and document:

```
ServiceGroupRoot/
├── ServiceModel.json          # Service topology definition
├── RolloutSpec.json          # Rollout orchestration spec
├── Templates/
│   ├── azuredeploy.json      # ARM templates
│   └── azuredeploy.bicep     # Bicep templates (if any)
├── Parameters/
│   ├── test.parameters.json
│   ├── ppe.parameters.json
│   └── prod.parameters.json
├── ScopeBindings/            # Subscription/region mappings
└── Extensions/               # EV2 extensions (if any)
```

**Action Items:**
1. ✅ Copy all ServiceModel.json files
2. ✅ Copy all RolloutSpec.json files
3. ✅ Copy all ARM/Bicep templates
4. ✅ Copy all parameter files
5. ✅ Document scope bindings
6. ✅ List all EV2 extensions used

> **Note:** These artifacts define what must be *expressed* in ConfigGen, not copied verbatim.

---

## 3. Create ConfigGen Project Structure

### Recommended CGv2 Layout

```
ConfigurationGeneration/
├── Topology/
│   ├── Environments/
│   │   ├── Test.cs           # Test environment config
│   │   ├── PPE.cs            # Pre-production environment
│   │   ├── Production.cs     # Production environment
│   │   └── Fairfax.cs        # Sovereign cloud (if applicable)
│   └── Topology.cs           # Main topology definition
├── Resources/
│   ├── AksCluster.cs         # AKS cluster resource
│   ├── StorageAccount.cs     # Storage account resource
│   ├── TableService.cs       # Table service resource
│   ├── EventHub.cs           # Event Hub resource
│   └── RoleAssignments.cs    # RBAC role assignments
├── Manifests/                # Generated EV2 artifacts (output)
└── Tests/                    # Validation tests
    ├── TopologyTests.cs
    └── ResourceTests.cs
```

### What ConfigGen Replaces

| EV2 Artifact | ConfigGen Equivalent |
|--------------|---------------------|
| ServiceModel.json | Topology.cs |
| ARM Parameters | Typed resource properties |
| Scope bindings | Constants + environment overrides |
| Region-specific files | Environment.DataCenters |

---

## 4. Convert EV2 ServiceModel to ConfigGen Topology

### EV2 Approach (Old)
- Regions, subscriptions, and resource groups repeated across files
- Region-specific JSON files
- Manual maintenance of consistency

### ConfigGen Approach (New)
- **One Topology** definition
- **Multiple Environment** files for environment-specific overrides
- **Region agnostic** by default
- EV2 Region Agnostic artifacts generated automatically

### Example Mapping

```csharp
// EV2 ServiceModel.json (Before)
{
  "contentVersion": "1.0.0.0",
  "serviceResourceGroupDefinitions": [
    {
      "name": "MyServiceRG",
      "location": "eastus",
      "subscriptionId": "12345-...",
      "serviceResourceDefinitions": [...]
    }
  ]
}

// ConfigGen Topology.cs (After)
public class Topology : TopologyBase
{
    public Topology()
    {
        var subscription = Subscription.Resolve("MyService-Prod");
        
        foreach (var dc in Environment.DataCenters)
        {
            var rg = new ResourceGroup("MyServiceRG", dc, subscription);
            // Add resources...
        }
    }
}
```

### Key Benefits
- Type safety at compile time
- Automatic subscription resolution
- Region-agnostic by design
- Reduced duplication

---

## 5. Convert ARM Templates to ConfigGen Resources

### Azure Service Level Conversion

For each Azure service, follow this pattern:

| ARM Concept | ConfigGen Equivalent |
|-------------|---------------------|
| ARM resource | ConfigGen Resource class |
| ARM parameters | Typed properties |
| Loops/conditionals | C# logic |

### Example: Storage Account

```json
// ARM Template (Before)
{
  "type": "Microsoft.Storage/storageAccounts",
  "apiVersion": "2021-04-01",
  "name": "[parameters('storageAccountName')]",
  "location": "[parameters('location')]",
  "sku": {
    "name": "[parameters('storageSku')]"
  },
  "kind": "StorageV2"
}
```

```csharp
// ConfigGen Resource (After)
public class StorageAccount : AzureResource
{
    public string Name { get; set; }
    public string Sku { get; set; }
    public string Kind { get; set; } = "StorageV2";
    
    public StorageAccount(string name, DataCenter dc, ResourceGroup rg)
    {
        Name = name;
        Location = dc.Region;
        ResourceGroup = rg;
        
        // Environment-specific overrides
        Sku = Environment.IsProduction 
            ? "Standard_GRS" 
            : "Standard_LRS";
    }
}
```

### Supported Azure Services

ConfigGen already has built-in support for:
- ✅ Storage Accounts
- ✅ Event Hubs
- ✅ AAD Applications
- ✅ Role Assignments (RBAC)
- ✅ AKS Clusters
- ✅ Key Vaults
- ✅ Virtual Networks

---

## 6. Handle Region Differences

### ❌ DON'T: Copy EV2 Files Per Region

```
# Bad Practice
ServiceGroupRoot/
├── ServiceModel.eastus.json
├── ServiceModel.westus.json
├── ServiceModel.italynorth.json
└── ServiceModel.fairfax.json
```

### ✅ DO: Use Environment Overrides

```csharp
public class Production : EnvironmentBase
{
    public Production()
    {
        // Default configuration
        var defaultSku = "Standard_D4s_v3";
        var defaultZones = new[] { "1", "2", "3" };
        
        // Region-specific overrides
        if (DataCenter.Region == "italynorth")
        {
            // Italy North may not support all zones
            defaultZones = new[] { "1", "2" };
        }
        
        if (DataCenter.Cloud == CloudType.Fairfax)
        {
            // Fairfax may have different SKU availability
            defaultSku = "Standard_D4_v3";
        }
        
        // CMK/KeyVault differences
        if (DataCenter.RequiresCMK)
        {
            EnableCustomerManagedKey = true;
            KeyVaultUri = GetRegionalKeyVault(DataCenter);
        }
    }
}
```

### Key Benefits
- **Compile-time validation** instead of runtime failures
- **Push errors left** (fail fast during build)
- **Huge reliability gain** for multi-region deployments
- **Single source of truth** for all regions

### Common Regional Differences to Handle

1. **SKU Availability**: Not all VM SKUs available in all regions
2. **Capacity Limits**: Different quota limits per region
3. **Availability Zones**: Not all regions support all zones
4. **CMK/KeyVault**: Different key management policies
5. **Networking**: VNET CIDR ranges per region
6. **Compliance**: Sovereign cloud requirements (Fairfax, Mooncake)

---

## 7. EV2 RolloutSpec Generation

### What ConfigGen Generates Automatically

ConfigGen produces **Region Agnostic EV2 (RA)** artifacts:

```
Manifests/
├── ServiceModel.json         # Generated - DO NOT EDIT
├── RolloutSpec.json          # Generated - DO NOT EDIT
├── Parameters/
│   ├── parameters.json       # Generated - DO NOT EDIT
│   └── scopebindings.json    # Generated - DO NOT EDIT
└── Templates/
    └── azuredeploy.json      # Generated - DO NOT EDIT
```

### What Your EV2 Pipeline Still Does

- Register artifacts with EV2 service
- Execute rollouts through EV2 orchestrator
- Enforce approval gates
- Manage ring progression
- Provide rollout visibility through EV2 portal

### Key Point
> **No behavioral change in EV2 orchestration**. ConfigGen simply produces the input artifacts that EV2 consumes.

---

## 8. EV2 Extensions and Scripts

### Evaluation Criteria

**Keep extensions only if:**
- Required for deployment logic not available through native Azure RPs
- Performing necessary post-deployment configuration
- Handling service-specific orchestration

**Remove or replace if:**
- Can be expressed through native Azure ARM/Bicep
- Performing parameter substitution (ConfigGen handles this)
- Doing region-specific logic (use environment overrides instead)

### How to Reference from ConfigGen

```csharp
public class Topology : TopologyBase
{
    public Topology()
    {
        // Reference existing EV2 extension
        var extension = new Ev2Extension
        {
            Name = "MyCustomExtension",
            ScriptPath = "Extensions/ConfigureService.ps1",
            Parameters = new Dictionary<string, string>
            {
                ["serviceEndpoint"] = $"https://{ServiceName}.{DataCenter.Region}.azure.net"
            }
        };
        
        AddExtension(extension);
    }
}
```

---

## 9. Pipeline Changes

### Before: Manual EV2 Artifacts

```yaml
# Old Pipeline
steps:
- task: PowerShell@2
  displayName: 'Generate ServiceModel'
  inputs:
    targetType: 'inline'
    script: |
      # Manual parameter substitution
      $json = Get-Content ServiceModel.template.json
      $json = $json -replace '{{region}}', '$(Region)'
      $json | Out-File ServiceModel.json

- task: EV2@1
  inputs:
    artifactPath: '$(Build.SourcesDirectory)/ServiceGroupRoot'
```

### After: ConfigGen-Based Pipeline

```yaml
# New Pipeline
steps:
- task: DotNetCoreCLI@2
  displayName: 'Build ConfigGen Project'
  inputs:
    command: 'build'
    projects: '**/ConfigurationGeneration.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Run ConfigGen'
  inputs:
    command: 'run'
    projects: '**/ConfigurationGeneration.csproj'
    arguments: '--output $(Build.ArtifactStagingDirectory)/Manifests'

- task: EV2@1
  displayName: 'Deploy with EV2'
  inputs:
    artifactPath: '$(Build.ArtifactStagingDirectory)/Manifests'
```

### What Remains Unchanged
- EV2 service connection configuration
- Approval gate definitions
- Ring progression policies
- Service tree registration

---

## 10. Validation and Rollout

### Compile-Time Validations

ConfigGen provides early validation for:

```csharp
// Type-safe configuration
public class Validation
{
    [ValidateAADApp]
    public string ServicePrincipalId { get; set; }
    
    [ValidateSku(Service = "AKS")]
    public string AksSku { get; set; }
    
    [ValidateKeyVault]
    public string KeyVaultUri { get; set; }
    
    [ValidateSubscription]
    public string SubscriptionId { get; set; }
}
```

### Local EV2 Generation

```bash
# Build ConfigGen project
dotnet build ConfigurationGeneration.csproj

# Generate EV2 artifacts locally
dotnet run --project ConfigurationGeneration.csproj \
  --environment Production \
  --output ./LocalManifests

# Validate generated artifacts
ev2 validate --path ./LocalManifests
```

### Commit Strategy

```bash
# Generated artifacts can be committed for traceability
git add Manifests/
git commit -m "Generated EV2 artifacts from ConfigGen for Production"

# Or ignored if regenerated in pipeline
echo "Manifests/" >> .gitignore
```

### Rollout Process

1. **Build**: ConfigGen generates EV2 RA artifacts
2. **Validate**: Compile-time checks + EV2 validation
3. **Commit**: Push generated artifacts (optional)
4. **Deploy**: EV2 executes rollout normally
5. **Monitor**: Use EV2 portal for rollout visibility

---

## Why ConfigGen for New Regions

### Benefits for Italy North, Fairfax, and New Regions

1. **Compile-Time Region Validation**: Know immediately if a region supports your configuration
2. **Type-Safe Configuration**: Catch errors before deployment
3. **Centralized Management**: One place to define all region differences
4. **Reduced Duplication**: No more copy-paste-modify across regions
5. **Automatic EV2 Generation**: No manual JSON editing
6. **Better Testing**: Unit test your configuration logic

### Example: Adding Italy North Support

```csharp
// Add to Environment configuration
public class Production : EnvironmentBase
{
    public Production()
    {
        DataCenters.Add(new DataCenter
        {
            Region = "italynorth",
            Zones = new[] { "1", "2" },  // Limited zones
            SupportsAvailabilityZones = true,
            RequiresCMK = true,
            MaxVmCount = 100  // Regional quota
        });
    }
}
```

---

## Migration Checklist

### Pre-Migration
- [ ] Inventory all existing EV2 artifacts
- [ ] Document current region deployments
- [ ] List all ARM templates and parameters
- [ ] Identify EV2 extensions in use
- [ ] Review current pipeline configuration

### Migration Phase 1: Setup
- [ ] Create ConfigGen project structure
- [ ] Set up Topology/ directory
- [ ] Set up Resources/ directory
- [ ] Set up Environments/ files
- [ ] Set up Tests/ directory

### Migration Phase 2: Convert Artifacts
- [ ] Convert ServiceModel to Topology.cs
- [ ] Convert ARM templates to Resource classes
- [ ] Convert parameter files to environment overrides
- [ ] Migrate scope bindings to constants
- [ ] Reference necessary EV2 extensions

### Migration Phase 3: Regional Support
- [ ] Define Test environment
- [ ] Define PPE environment
- [ ] Define Production environment
- [ ] Add Fairfax support (if applicable)
- [ ] Add Italy North support (if applicable)
- [ ] Implement region-specific overrides

### Migration Phase 4: Pipeline
- [ ] Update build steps to run ConfigGen
- [ ] Configure artifact output path
- [ ] Update EV2 task to use generated artifacts
- [ ] Preserve approval gates
- [ ] Test pipeline end-to-end

### Migration Phase 5: Validation
- [ ] Run compile-time validation
- [ ] Generate EV2 artifacts locally
- [ ] Validate with EV2 tooling
- [ ] Test deployment to Test environment
- [ ] Test deployment to PPE
- [ ] Plan Production rollout

### Post-Migration
- [ ] Archive old EV2 JSON files
- [ ] Update team documentation
- [ ] Train team on ConfigGen usage
- [ ] Monitor first production deployment
- [ ] Iterate based on feedback

---

## Troubleshooting

### Common Issues

#### Issue: "Subscription not found"
```csharp
// Solution: Use subscription resolver
var subscription = Subscription.Resolve("MyService-Prod");
// Ensure subscription is registered in ConfigGen
```

#### Issue: "Region not supported"
```csharp
// Solution: Add region to environment DataCenters
DataCenters.Add(new DataCenter { Region = "italynorth", ... });
```

#### Issue: "SKU not available in region"
```csharp
// Solution: Use environment-specific override
Sku = DataCenter.Region == "italynorth" 
    ? "Standard_D4_v3"  // Fallback SKU
    : "Standard_D4s_v3"; // Preferred SKU
```

#### Issue: "Generated ARM template invalid"
```bash
# Validate locally before deployment
az deployment group validate \
  --resource-group test-rg \
  --template-file Manifests/Templates/azuredeploy.json \
  --parameters @Manifests/Parameters/parameters.json
```

---

## Additional Resources

### Documentation
- [EV2 as Orchestration Mechanism](https://aka.ms/ev2-orchestration)
- [Azure Service ConfigGen Extensions](https://aka.ms/configgen-extensions)
- [Region Agnostic EV2 Management](https://aka.ms/ev2-ra)
- [Quick Guide to EV2 Onboarding](https://aka.ms/ev2-onboarding)

### Support
- ConfigGen Team: [configgen-support@microsoft.com](mailto:configgen-support@microsoft.com)
- EV2 Team: [ev2-support@microsoft.com](mailto:ev2-support@microsoft.com)

---

## Summary

### The One-Liner
> **ConfigGen authors intent, EV2 executes rollout.**

### Key Takeaways
1. ✅ EV2 remains your deployment orchestrator
2. ✅ ConfigGen becomes your configuration source of truth
3. ✅ Region differences handled at compile time
4. ✅ Type safety prevents runtime errors
5. ✅ Pipeline changes are minimal
6. ✅ Perfect for new regions (Italy North) and sovereign clouds (Fairfax)

---

*For specific service migrations or additional support, please contact the ConfigGen team.*
