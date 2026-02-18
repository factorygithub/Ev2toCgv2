# EV2 to ConfigGen v2 Migration Repository

This repository provides a comprehensive guide and reference implementation for migrating from manually authored EV2 (Express V2) deployment artifacts to ConfigGen v2 (CGv2) as the source of truth for Azure service deployments.

## Overview

**ConfigGen authors intent, EV2 executes rollout.**

This repository demonstrates how to transform from hand-crafted EV2 JSON files to type-safe, compile-time validated ConfigGen configurations, while keeping EV2 as the deployment orchestrator.

## 📚 Documentation

- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Comprehensive step-by-step migration guide (Start here!)
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Quick reference for common scenarios and troubleshooting
- **[PIPELINE_EXAMPLES.md](PIPELINE_EXAMPLES.md)** - Azure DevOps pipeline configurations (before/after)

## 🏗️ Repository Structure

```
Ev2toCgv2/
├── README.md                           # This file
├── MIGRATION_GUIDE.md                  # Complete migration documentation
├── QUICK_REFERENCE.md                  # Quick start guide
├── PIPELINE_EXAMPLES.md                # Pipeline configuration examples
│
├── ConfigurationGeneration/            # ✨ New ConfigGen structure
│   ├── Topology/
│   │   ├── Topology.cs                # Main service topology definition
│   │   └── Environments/
│   │       ├── EnvironmentBase.cs     # Base environment class
│   │       ├── Test.cs                # Test environment config
│   │       ├── PPE.cs                 # Pre-production config
│   │       ├── Production.cs          # Production config (includes Italy North)
│   │       └── Fairfax.cs             # Sovereign cloud config
│   ├── Resources/
│   │   ├── AksCluster.cs              # AKS resource definition
│   │   ├── StorageAccount.cs          # Storage resource definition
│   │   ├── TableService.cs            # Table service definition
│   │   ├── EventHub.cs                # Event Hub definition
│   │   └── RoleAssignments.cs         # RBAC role assignments
│   ├── Manifests/                     # Generated EV2 artifacts (output)
│   └── Tests/                         # Validation tests
│
└── SampleEv2Service/                   # 📦 Example of old EV2 structure
    └── ServiceGroupRoot/
        ├── ServiceModel.json          # Old: Hand-authored service model
        ├── RolloutSpec.json           # Old: Hand-authored rollout spec
        ├── Templates/
        │   └── storage.json           # Old: ARM templates
        └── Parameters/
            ├── storage.eastus.parameters.json    # Old: Per-region params
            └── storage.westus2.parameters.json   # Old: Per-region params
```

## 🚀 Quick Start

### 1. Review the Documentation
```bash
# Start with the migration guide
cat MIGRATION_GUIDE.md

# Quick reference for common scenarios
cat QUICK_REFERENCE.md
```

### 2. Explore the ConfigGen Structure
```bash
# View the topology definition
cat ConfigurationGeneration/Topology/Topology.cs

# Check environment configurations
cat ConfigurationGeneration/Topology/Environments/Production.cs
```

### 3. Compare Old vs New
```bash
# Old approach: Manual EV2 artifacts
ls -la SampleEv2Service/ServiceGroupRoot/

# New approach: ConfigGen structure
ls -la ConfigurationGeneration/
```

## ✨ Key Benefits

### Before: Manual EV2
- ❌ Hand-authored JSON files per region
- ❌ Manual parameter substitution
- ❌ Runtime errors (deployment fails)
- ❌ High duplication and inconsistency
- ❌ Difficult to add new regions
- ❌ Complex sovereign cloud support

### After: ConfigGen v2
- ✅ Single source of truth
- ✅ Type-safe C# configuration
- ✅ Compile-time validation
- ✅ Automatic region handling
- ✅ Easy region addition (30 minutes)
- ✅ Built-in sovereign cloud support

## 🎯 Use Cases

### Adding Italy North
```csharp
// Just add this to Production.cs
DataCenters.Add(new DataCenter
{
    Region = "italynorth",
    Zones = new[] { "1", "2" }, // Limited zones
    MaxNodeCount = 8,
    RequiresCMK = true
});
```

### Adding Fairfax (Sovereign Cloud)
```csharp
// Create Fairfax.cs environment
public class Fairfax : EnvironmentBase
{
    public Fairfax()
    {
        DataCenters.Add(new DataCenter
        {
            Region = "usgovvirginia",
            Cloud = CloudType.Fairfax,
            RequiresCMK = true
        });
    }
}
```

### Handling Region Differences
```csharp
// Environment-specific logic
if (environment.IsProduction)
    Sku = dataCenter.SupportsZoneRedundancy ? "Standard_ZRS" : "Standard_GRS";
else
    Sku = "Standard_LRS"; // Cost savings for non-prod
```

## 📋 Migration Checklist

- [ ] Read MIGRATION_GUIDE.md
- [ ] Inventory existing EV2 artifacts
- [ ] Create ConfigGen project structure
- [ ] Convert ServiceModel → Topology.cs
- [ ] Convert ARM templates → Resource classes
- [ ] Define environment configurations
- [ ] Add region-specific overrides
- [ ] Update pipeline configuration
- [ ] Test in non-production
- [ ] Deploy to production

## 🔧 What Changes vs What Stays

### ✅ What Does NOT Change
- EV2 as deployment orchestrator
- EV2 portal and monitoring
- Approval workflows and gates
- Ring progression policies
- ADO EV2 pipelines (minimal changes)
- Service registration

### 🔄 What Changes
- EV2 artifacts are now **generated** by ConfigGen
- Configuration is **type-safe C#** instead of JSON
- Validation happens at **compile-time** instead of deployment-time
- **Single topology** instead of per-region files
- Region differences handled via **environment overrides**

## 📊 Time Estimates

| Service Size | Setup | Conversion | Testing | Total |
|--------------|-------|------------|---------|-------|
| Small (1-2 resources, 2-3 regions) | 2h | 4h | 3h | **1-2 days** |
| Medium (5-10 resources, 5-10 regions) | 3h | 8h | 5h | **2-3 days** |
| Large (10+ resources, 10+ regions) | 4h | 16h | 8h | **1 week** |

## 🎓 Training Resources

### Internal Documentation
- [EV2 as Orchestration Mechanism (PowerPoint)](https://aka.ms/ev2-orchestration)
- [Azure Service ConfigGen Extensions (Word)](https://aka.ms/configgen-extensions)
- [EV2 Region Agnostic Management (Word)](https://aka.ms/ev2-ra)
- [Quick Guide to EV2 Onboarding (Word)](https://aka.ms/ev2-onboarding)

### Key Concepts
1. **Topology** - Defines your service structure (replaces ServiceModel.json)
2. **Environment** - Defines environment-specific configurations
3. **DataCenter** - Represents a region with its capabilities
4. **Resource** - Type-safe Azure resource definition
5. **Region Agnostic** - One definition works for all regions

## 🆘 Support

- **ConfigGen Team:** configgen-support@microsoft.com
- **EV2 Team:** ev2-support@microsoft.com
- **Issues:** Use GitHub Issues in this repository

## 🤝 Contributing

This is a reference repository. For questions or improvements:
1. Review existing documentation
2. Check QUICK_REFERENCE.md for troubleshooting
3. Contact the ConfigGen team

## 📜 License

This project is provided as internal Microsoft guidance for EV2 to ConfigGen v2 migration.

---

## 🎬 Next Steps

1. **Read** the [Migration Guide](MIGRATION_GUIDE.md)
2. **Explore** the ConfigGen structure in `ConfigurationGeneration/`
3. **Compare** with old EV2 structure in `SampleEv2Service/`
4. **Plan** your migration using the checklist above
5. **Execute** following the step-by-step guide

---

*Last Updated: 2026-02-18*
