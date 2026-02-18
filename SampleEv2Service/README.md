# Sample EV2 Service (Legacy Structure)

This directory contains an example of the **old approach** - manually authored EV2 artifacts.

## ⚠️ This is the OLD Way

This structure demonstrates what you're migrating **FROM**. Do NOT use this as a template for new services.

## Directory Structure

```
SampleEv2Service/
└── ServiceGroupRoot/
    ├── ServiceModel.json           # Hand-authored service model
    ├── RolloutSpec.json           # Hand-authored rollout specification
    ├── Templates/                 # ARM templates
    │   ├── storage.json
    │   ├── aks.json
    │   └── eventhub.json
    ├── Parameters/                # Per-region parameter files
    │   ├── storage.eastus.parameters.json
    │   ├── storage.westus2.parameters.json
    │   ├── aks.eastus.parameters.json
    │   └── aks.westus2.parameters.json
    └── Extensions/                # EV2 extensions (if any)
        └── scripts/
```

## Problems with This Approach

### 1. Manual JSON Maintenance
```json
// ServiceModel.json - Must manually maintain for each region
{
  "serviceResourceGroupDefinitions": [
    {
      "name": "MyAzureService-eastus-rg",
      "location": "eastus",
      ...
    },
    {
      "name": "MyAzureService-westus2-rg",  // Copy-paste
      "location": "westus2",
      ...
    }
  ]
}
```

**Problems:**
- ❌ High duplication
- ❌ Easy to introduce inconsistencies
- ❌ Prone to copy-paste errors
- ❌ No validation until deployment

### 2. Per-Region Parameter Files

```
Parameters/
├── storage.eastus.parameters.json      # Duplicate definitions
├── storage.westus2.parameters.json     # Different values
├── storage.northeurope.parameters.json # More copies
└── storage.italynorth.parameters.json  # Even more copies
```

**Problems:**
- ❌ N files for N regions
- ❌ Difficult to update globally
- ❌ Easy to miss a region
- ❌ No compile-time validation

### 3. Manual Parameter Substitution

Often requires pipeline scripts:

```powershell
# Pipeline script to substitute parameters
$params = Get-Content 'storage.eastus.parameters.json' | ConvertFrom-Json
$params.parameters.location.value = "eastus"
$params.parameters.sku.value = "Standard_GRS"
$params | ConvertTo-Json -Depth 10 | Set-Content 'storage.eastus.parameters.json'
```

**Problems:**
- ❌ Error-prone string manipulation
- ❌ No type safety
- ❌ Hard to maintain
- ❌ Difficult to test

### 4. Adding New Regions is Painful

To add Italy North:
1. Copy ServiceModel.json section
2. Change region name to "italynorth"
3. Copy all parameter files
4. Rename and update each parameter file
5. Update RolloutSpec.json
6. Test deployment

**Time:** 2-3 hours  
**Risk:** High (easy to miss something)

### 5. No Compile-Time Validation

```json
{
  "sku": "Standrd_GRS"  // Typo! Won't be caught until deployment
}
```

**Problems:**
- ❌ Errors discovered during deployment
- ❌ Wasted time on failed deployments
- ❌ Potential production impact

## Comparison: Old vs New

### Adding Italy North Region

#### Old Approach (This Directory)
```bash
# 1. Copy and modify ServiceModel.json (10 min)
# 2. Copy and modify each parameter file (20 min)
# 3. Update RolloutSpec.json (10 min)
# 4. Test deployment (1-2 hours)
# Total: 2-3 hours + risk of errors
```

#### New Approach (ConfigGen)
```csharp
// In Production.cs - add one block (5 min)
DataCenters.Add(new DataCenter
{
    Region = "italynorth",
    Zones = new[] { "1", "2" },
    MaxNodeCount = 8
});
// Build, validate, done (5 min)
// Total: 10-15 minutes, compile-time validated
```

### Global SKU Update

#### Old Approach
```bash
# Find all parameter files
find . -name "*.parameters.json" | while read file; do
  # Manually edit each file
  # Update SKU value
  # Save
done
# Risk: Missing a file, typos, inconsistencies
```

#### New Approach
```csharp
// One line change in StorageAccount class
Sku = environment.IsProduction ? "Standard_ZRS" : "Standard_LRS";
// All regions updated automatically
```

## Migration Path

To migrate from this structure to ConfigGen:

### 1. Inventory
- ✅ ServiceModel.json → Understand service structure
- ✅ RolloutSpec.json → Understand deployment order
- ✅ Templates/ → Understand resources
- ✅ Parameters/ → Understand configurations

### 2. Map to ConfigGen
- ServiceModel.json → Topology.cs
- Regions list → Environment.DataCenters
- ARM templates → Resource classes
- Parameter files → Environment overrides

### 3. Convert
See `MIGRATION_GUIDE.md` for detailed steps.

### 4. Validate
- Build ConfigGen project
- Compare generated vs original artifacts
- Test in non-production environment

### 5. Deploy
- Update pipeline to use ConfigGen
- Deploy with EV2 (same as before)

## Why Migrate?

### The Old Way (This Directory)
- 📝 100+ lines of JSON per region
- 🔄 10+ files to maintain per region
- ⏰ 2-3 hours to add a region
- ❌ Runtime error detection
- 😰 High maintenance burden

### The New Way (ConfigGen)
- 📝 10-20 lines of C# per region
- 🔄 1 file to maintain per region
- ⏰ 15 minutes to add a region
- ✅ Compile-time validation
- 😊 Low maintenance burden

## Example Files

### ServiceModel.json
See `ServiceGroupRoot/ServiceModel.json` for a complete example of manually authored service model.

**Key problems:**
- Duplicated structure per region
- Manual subscription ID management
- No validation of region-specific constraints

### RolloutSpec.json
See `ServiceGroupRoot/RolloutSpec.json` for rollout orchestration.

**Key problems:**
- Must manually define deployment order
- Hard-coded region names
- Difficult to add conditional logic

### Templates/storage.json
See `ServiceGroupRoot/Templates/storage.json` for ARM template.

**Note:** ARM templates remain similar, but ConfigGen generates them from C# classes.

### Parameters/*.parameters.json
See `ServiceGroupRoot/Parameters/` for region-specific parameters.

**Key problems:**
- One file per region per resource
- No shared logic
- Easy to drift

## Conclusion

This directory shows what **NOT** to do. It's preserved here for:
1. Understanding the migration source
2. Comparing old vs new approaches
3. Training purposes

**For new services or migrations, use the ConfigGen structure in `ConfigurationGeneration/`.**

---

See `MIGRATION_GUIDE.md` for step-by-step migration instructions.
