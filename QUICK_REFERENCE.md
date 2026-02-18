# EV2 to ConfigGen v2 Migration - Quick Reference

## Quick Start Checklist

### Phase 1: Setup (1-2 hours)
- [ ] Clone this repository
- [ ] Review `MIGRATION_GUIDE.md`
- [ ] Inventory your existing EV2 artifacts
- [ ] Set up ConfigGen project structure

### Phase 2: Configuration (2-4 hours per service)
- [ ] Convert ServiceModel.json → Topology.cs
- [ ] Convert ARM templates → Resource classes
- [ ] Define environment configurations
- [ ] Add region-specific overrides
- [ ] Configure role assignments

### Phase 3: Testing (2-3 hours)
- [ ] Build ConfigGen project locally
- [ ] Generate EV2 artifacts
- [ ] Validate with EV2 tools
- [ ] Test in non-production environment

### Phase 4: Pipeline Integration (1-2 hours)
- [ ] Update build pipeline
- [ ] Configure ConfigGen execution
- [ ] Test end-to-end deployment

### Phase 5: Production (Plan for 1 week)
- [ ] Deploy to PPE
- [ ] Monitor for 24-48 hours
- [ ] Deploy to Production (staged rollout)
- [ ] Monitor and iterate

---

## Common Scenarios

### Scenario 1: Adding a New Region (e.g., Italy North)

```csharp
// In Production.cs
DataCenters.Add(new DataCenter
{
    Region = "italynorth",
    Cloud = CloudType.Public,
    SupportsAvailabilityZones = true,
    Zones = new[] { "1", "2" }, // Limited zones
    SupportsAks = true,
    SupportsZoneRedundancy = true,
    RequiresCMK = true,
    MaxNodeCount = 8,
    MaxPartitions = 16,
    KeyVaultUri = "https://keyvault-italynorth.vault.azure.net/"
});
```

**Time to complete:** 30 minutes  
**Result:** Compile-time validation of region compatibility

### Scenario 2: Adding Sovereign Cloud (e.g., Fairfax)

```csharp
// Create new Fairfax.cs environment
public class Fairfax : EnvironmentBase
{
    public Fairfax()
    {
        Name = "Fairfax";
        IsProduction = true;
        TenantId = "fairfax-tenant-id";
        
        DataCenters.Add(new DataCenter
        {
            Region = "usgovvirginia",
            Cloud = CloudType.Fairfax,
            RequiresCMK = true, // Government compliance
            // ... additional config
        });
    }
}
```

**Time to complete:** 1-2 hours  
**Result:** Separate topology for sovereign cloud with compliance requirements

### Scenario 3: Converting Existing ARM Template

**Before (ARM):**
```json
{
  "type": "Microsoft.Storage/storageAccounts",
  "name": "[parameters('storageAccountName')]",
  "location": "[parameters('location')]",
  "sku": { "name": "[parameters('sku')]" }
}
```

**After (ConfigGen):**
```csharp
var storage = new StorageAccount(
    name: $"{ServiceName}-{dataCenter.Region}-sa",
    region: dataCenter.Region,
    resourceGroup: resourceGroup
);

// Environment-specific logic
storage.ApplyConfiguration(environment, dataCenter);
```

**Time to complete:** 15-30 minutes per resource  
**Result:** Type-safe, compile-time validated configuration

---

## Key Concepts

### 1. Region Agnostic by Default
ConfigGen automatically generates region-specific configurations from a single topology definition.

### 2. Compile-Time Validation
Errors are caught during build, not deployment. This is a huge reliability improvement.

### 3. Environment Overrides
```csharp
if (environment.IsProduction)
    Sku = "Premium";
else
    Sku = "Standard";
```

### 4. Type Safety
```csharp
// This won't compile if property doesn't exist
storage.MinimumTlsVersion = "TLS1_2";
```

---

## Troubleshooting Guide

### Problem: "Cannot resolve subscription"
**Solution:**
```csharp
// Use the subscription resolver
var subscription = Subscription.Resolve("MyService-Prod");
```

### Problem: "SKU not available in region"
**Solution:**
```csharp
// Add region-specific override
VmSize = dataCenter.Region == "italynorth" 
    ? "Standard_D4_v3"  // Fallback
    : "Standard_D4s_v3"; // Preferred
```

### Problem: "Zone not supported"
**Solution:**
```csharp
// Check zone support before configuring
if (dataCenter.SupportsAvailabilityZones)
    AvailabilityZones = dataCenter.Zones;
```

### Problem: "Generated ARM template validation fails"
**Solution:**
```bash
# Validate locally before deployment
az deployment group validate \
  --resource-group test-rg \
  --template-file Manifests/Templates/azuredeploy.json \
  --parameters @Manifests/Parameters/parameters.json
```

---

## Time Estimates

### Small Service (1-2 resources, 2-3 regions)
- **Setup:** 2 hours
- **Conversion:** 4 hours
- **Testing:** 3 hours
- **Total:** 1-2 days

### Medium Service (5-10 resources, 5-10 regions)
- **Setup:** 3 hours
- **Conversion:** 8 hours
- **Testing:** 5 hours
- **Total:** 2-3 days

### Large Service (10+ resources, 10+ regions, multiple clouds)
- **Setup:** 4 hours
- **Conversion:** 16 hours
- **Testing:** 8 hours
- **Total:** 1 week

---

## Benefits Summary

| Aspect | Before (Manual EV2) | After (ConfigGen) |
|--------|-------------------|------------------|
| **Region Addition** | Copy-paste JSON, edit manually (2-3 hours) | Add DataCenter config (30 min) |
| **Error Detection** | Runtime (deployment fails) | Compile-time (build fails) |
| **Consistency** | Manual, error-prone | Automatic, type-safe |
| **Duplication** | High (per region files) | Low (single topology) |
| **Maintenance** | Difficult | Easy |
| **Sovereign Clouds** | Complex | Straightforward |

---

## Key Files Reference

```
Repository Structure:
├── MIGRATION_GUIDE.md          ← Comprehensive guide
├── QUICK_REFERENCE.md          ← This file
├── ConfigurationGeneration/     ← New ConfigGen structure
│   ├── Topology/
│   │   ├── Topology.cs         ← Main service definition
│   │   └── Environments/       ← Environment configs
│   ├── Resources/              ← Azure resource classes
│   ├── Manifests/              ← Generated EV2 artifacts
│   └── Tests/                  ← Validation tests
└── SampleEv2Service/           ← Example of old EV2 structure
    └── ServiceGroupRoot/
        ├── ServiceModel.json
        ├── RolloutSpec.json
        └── Templates/
```

---

## Next Steps

1. **Read:** Start with `MIGRATION_GUIDE.md` for comprehensive details
2. **Explore:** Review the sample ConfigGen structure in `ConfigurationGeneration/`
3. **Compare:** Look at `SampleEv2Service/` to see the old approach
4. **Plan:** Use the checklist at the top of this document
5. **Execute:** Follow the migration guide step by step

---

## Support

- **ConfigGen Team:** configgen-support@microsoft.com
- **EV2 Team:** ev2-support@microsoft.com
- **Documentation:** See `MIGRATION_GUIDE.md`

---

*Last Updated: 2026-02-18*
