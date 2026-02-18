# Architecture & Flow Diagrams

This document provides visual representations of the EV2 to ConfigGen v2 migration architecture and workflow.

## Table of Contents
1. [Overall Architecture](#overall-architecture)
2. [Before vs After Flow](#before-vs-after-flow)
3. [ConfigGen Generation Flow](#configgen-generation-flow)
4. [Deployment Pipeline Flow](#deployment-pipeline-flow)
5. [Region Addition Flow](#region-addition-flow)

---

## Overall Architecture

### High-Level View

```
┌─────────────────────────────────────────────────────────────┐
│                    EV2 to ConfigGen v2                       │
│                                                               │
│  ┌───────────────┐              ┌───────────────┐           │
│  │   ConfigGen   │   generates  │  EV2 Artifacts│           │
│  │  (Source of   │─────────────▶│  (ServiceModel│           │
│  │    Truth)     │              │  RolloutSpec) │           │
│  └───────────────┘              └───────┬───────┘           │
│         ▲                                │                    │
│         │                                ▼                    │
│  ┌──────┴────────┐              ┌───────────────┐           │
│  │   Developer   │              │      EV2      │           │
│  │  Authors C#   │              │ Orchestrator  │           │
│  │Configuration  │              │   (Deploys)   │           │
│  └───────────────┘              └───────────────┘           │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Key Principle
```
┌─────────────┐      ┌─────────────┐      ┌──────────────┐
│  ConfigGen  │      │     EV2     │      │    Azure     │
│   Authors   │─────▶│  Executes   │─────▶│   Resources  │
│   Intent    │      │   Rollout   │      │  (Deployed)  │
└─────────────┘      └─────────────┘      └──────────────┘
```

---

## Before vs After Flow

### Before: Manual EV2 Artifacts

```
Developer                Build Pipeline              EV2 Deployment
    │                           │                           │
    │  Edit JSON files          │                           │
    │  (ServiceModel,           │                           │
    │   RolloutSpec,            │                           │
    │   Parameters)             │                           │
    ├──────────────────────────▶│                           │
    │                           │                           │
    │                           │  Copy JSON files          │
    │                           │  Substitute params        │
    │                           │  (manual/scripts)         │
    │                           ├──────────────────────────▶│
    │                           │                           │
    │                           │                           │  Deploy
    │                           │                           │  (may fail
    │                           │                           │   at runtime)
    │                           │                           │
    │◀──────────────────────────┼───────────────────────────┤
    │   Fix errors              │                           │
    │   Edit JSON again         │                           │
    └──────────────────────────▶│                           │

Problems:
❌ Manual JSON editing (error-prone)
❌ Runtime error detection
❌ High duplication
❌ Difficult maintenance
```

### After: ConfigGen-Based

```
Developer                Build Pipeline              EV2 Deployment
    │                           │                           │
    │  Edit C# classes          │                           │
    │  (Topology,               │                           │
    │   Environments,           │                           │
    │   Resources)              │                           │
    ├──────────────────────────▶│                           │
    │                           │                           │
    │                           │  dotnet build             │
    │                           │  (compile-time            │
    │                           │   validation)             │
    │                           │  ✓                        │
    │                           │                           │
    │                           │  dotnet run               │
    │                           │  (generate EV2            │
    │                           │   artifacts)              │
    │                           ├──────────────────────────▶│
    │                           │                           │
    │                           │                           │  Deploy
    │                           │                           │  (validated
    │                           │                           │   at compile)
    │                           │                           │  ✓
    │                           │                           │
    │◀──────────────────────────┴───────────────────────────┘
    │   Success!                │                           │

Benefits:
✅ Type-safe C# editing (IntelliSense)
✅ Compile-time error detection
✅ Single source of truth
✅ Easy maintenance
```

---

## ConfigGen Generation Flow

### Detailed Generation Process

```
┌─────────────────────────────────────────────────────────────┐
│                ConfigGen Build & Generate                    │
└─────────────────────────────────────────────────────────────┘

Step 1: Define Configuration (Developer)
┌──────────────────────────────────────────────────────────┐
│  Topology.cs                                              │
│  ├─ Service Definition                                    │
│  ├─ Resource Groups                                       │
│  └─ Resources per Region                                  │
│                                                            │
│  Environments/                                            │
│  ├─ Test.cs         (2 regions, basic SKUs)              │
│  ├─ PPE.cs          (3 regions, production-like)         │
│  ├─ Production.cs   (6+ regions, full scale)             │
│  └─ Fairfax.cs      (3 gov regions, compliance)          │
│                                                            │
│  Resources/                                               │
│  ├─ AksCluster.cs   (type-safe AKS config)              │
│  ├─ StorageAccount.cs                                    │
│  ├─ EventHub.cs                                          │
│  └─ RoleAssignments.cs                                   │
└──────────────────────────────────────────────────────────┘
                           ▼
Step 2: Build (dotnet build)
┌──────────────────────────────────────────────────────────┐
│  Compile-Time Validation                                  │
│  ✓ Type checking                                         │
│  ✓ Property validation                                   │
│  ✓ Reference validation                                  │
│  ✓ Logic validation                                      │
│                                                            │
│  If errors: Fail fast at compile time ❌                │
│  If success: Continue to generate ✓                      │
└──────────────────────────────────────────────────────────┘
                           ▼
Step 3: Generate (dotnet run --environment Production)
┌──────────────────────────────────────────────────────────┐
│  For each DataCenter in Environment.DataCenters:          │
│  ├─ Create ResourceGroup                                 │
│  ├─ Apply environment overrides                          │
│  ├─ Apply region-specific config                         │
│  ├─ Generate ARM templates                               │
│  └─ Generate parameters                                   │
│                                                            │
│  Aggregate into EV2 Region Agnostic format:              │
│  ├─ ServiceModel.json                                    │
│  ├─ RolloutSpec.json                                     │
│  ├─ Templates/*.json                                     │
│  └─ Parameters/*.json                                    │
└──────────────────────────────────────────────────────────┘
                           ▼
Step 4: Output
┌──────────────────────────────────────────────────────────┐
│  Manifests/                                               │
│  ├─ ServiceModel.json      (Generated - DO NOT EDIT)    │
│  ├─ RolloutSpec.json       (Generated - DO NOT EDIT)    │
│  ├─ Templates/                                           │
│  │   ├─ storage.json                                    │
│  │   ├─ aks.json                                        │
│  │   └─ eventhub.json                                   │
│  └─ Parameters/                                          │
│      ├─ parameters.json                                  │
│      └─ scopebindings.json                               │
└──────────────────────────────────────────────────────────┘
```

---

## Deployment Pipeline Flow

### Complete CI/CD Flow

```
┌────────────┐
│   Commit   │  Developer commits C# configuration changes
│  Changes   │
└─────┬──────┘
      │
      ▼
┌────────────────────────────────────────────────────────┐
│  Build Stage                                            │
│  ┌──────────────────────────────────────────────────┐ │
│  │  1. dotnet restore                                │ │
│  │  2. dotnet build                                  │ │
│  │     ├─ Compile-time validation                    │ │
│  │     └─ Unit tests                                 │ │
│  │  3. dotnet run --environment Test                 │ │
│  │     └─ Generate Test EV2 artifacts                │ │
│  │  4. dotnet run --environment Production           │ │
│  │     └─ Generate Production EV2 artifacts          │ │
│  │  5. Publish artifacts                             │ │
│  └──────────────────────────────────────────────────┘ │
└─────────────────────────┬──────────────────────────────┘
                          ▼
┌────────────────────────────────────────────────────────┐
│  Deploy Test Stage                                      │
│  ┌──────────────────────────────────────────────────┐ │
│  │  1. Download test artifacts                       │ │
│  │  2. EV2 Deploy                                    │ │
│  │     ├─ Register artifacts                         │ │
│  │     ├─ Execute rollout (Test regions)            │ │
│  │     └─ Monitor deployment                         │ │
│  │  3. Validate deployment                           │ │
│  └──────────────────────────────────────────────────┘ │
└─────────────────────────┬──────────────────────────────┘
                          ▼
┌────────────────────────────────────────────────────────┐
│  Deploy PPE Stage                                       │
│  ┌──────────────────────────────────────────────────┐ │
│  │  1. Download production artifacts                 │ │
│  │  2. Manual approval gate                          │ │
│  │  3. EV2 Deploy                                    │ │
│  │     ├─ Execute rollout (PPE regions)             │ │
│  │     └─ Monitor deployment                         │ │
│  │  4. Validate 24-48 hours                         │ │
│  └──────────────────────────────────────────────────┘ │
└─────────────────────────┬──────────────────────────────┘
                          ▼
┌────────────────────────────────────────────────────────┐
│  Deploy Production Stage                                │
│  ┌──────────────────────────────────────────────────┐ │
│  │  1. Download production artifacts                 │ │
│  │  2. Manual approval gate (required)              │ │
│  │  3. EV2 Deploy                                    │ │
│  │     ├─ Ring 0 (pilot)                            │ │
│  │     ├─ Wait + approval                            │ │
│  │     ├─ Ring 1 (25%)                               │ │
│  │     ├─ Wait + approval                            │ │
│  │     ├─ Ring 2 (50%)                               │ │
│  │     ├─ Wait + approval                            │ │
│  │     └─ Ring 3 (100%)                              │ │
│  │  4. Monitor and validate                          │ │
│  └──────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
```

---

## Region Addition Flow

### Adding Italy North (Example)

```
Step 1: Update Production Environment (5 minutes)
┌──────────────────────────────────────────────────────┐
│  File: Environments/Production.cs                     │
│                                                        │
│  DataCenters.Add(new DataCenter                       │
│  {                                                     │
│      Region = "italynorth",                           │
│      Zones = new[] { "1", "2" },    // Limited zones │
│      SupportsAks = true,                              │
│      MaxNodeCount = 8,               // Regional quota│
│      RequiresCMK = true,             // Compliance    │
│      KeyVaultUri = "https://kv-italynorth..."        │
│  });                                                   │
└──────────────────────────────────────────────────────┘
                      ▼
Step 2: Build & Validate (2 minutes)
┌──────────────────────────────────────────────────────┐
│  $ dotnet build                                       │
│                                                        │
│  ✓ Compile-time validation                           │
│  ✓ Type checking                                     │
│  ✓ Unit tests pass                                   │
│                                                        │
│  If errors: Fix immediately                           │
│  If success: Continue                                 │
└──────────────────────────────────────────────────────┘
                      ▼
Step 3: Generate Artifacts (1 minute)
┌──────────────────────────────────────────────────────┐
│  $ dotnet run --environment Production                │
│                                                        │
│  Generates:                                           │
│  ├─ ServiceModel.json (includes Italy North)         │
│  ├─ RolloutSpec.json (includes Italy North)          │
│  ├─ Templates/ (ARM templates for Italy North)       │
│  └─ Parameters/ (Italy North parameters)             │
└──────────────────────────────────────────────────────┘
                      ▼
Step 4: Review & Commit (2 minutes)
┌──────────────────────────────────────────────────────┐
│  $ git diff Environments/Production.cs                │
│  $ git commit -m "Add Italy North support"           │
│  $ git push                                           │
└──────────────────────────────────────────────────────┘
                      ▼
Step 5: Deploy via Pipeline (automated)
┌──────────────────────────────────────────────────────┐
│  Pipeline triggered:                                  │
│  ├─ Build with Italy North config                    │
│  ├─ Deploy to Test (if Italy North in Test)         │
│  ├─ Deploy to PPE (if Italy North in PPE)           │
│  └─ Deploy to Production (Italy North included)      │
│                                                        │
│  EV2 handles rollout orchestration                    │
└──────────────────────────────────────────────────────┘

Total Time: ~10-15 minutes
Compare to Manual EV2: 2-3 hours
```

### Side-by-Side Comparison

```
┌─────────────────────────┬─────────────────────────┐
│   Manual EV2 (OLD)      │   ConfigGen (NEW)       │
├─────────────────────────┼─────────────────────────┤
│ 1. Copy ServiceModel    │ 1. Add DataCenter block │
│    section (10 min)     │    (5 min)              │
│                         │                         │
│ 2. Edit region name     │ 2. dotnet build         │
│    (5 min)              │    (2 min)              │
│                         │                         │
│ 3. Copy all parameter   │ 3. dotnet run           │
│    files (20 min)       │    (1 min)              │
│                         │                         │
│ 4. Update each file     │ 4. git commit           │
│    (30 min)             │    (2 min)              │
│                         │                         │
│ 5. Update RolloutSpec   │ 5. Done! ✓              │
│    (10 min)             │                         │
│                         │                         │
│ 6. Manual validation    │                         │
│    (20 min)             │                         │
│                         │                         │
│ 7. Test deploy          │                         │
│    (1-2 hours)          │                         │
│                         │                         │
│ Total: 2-3 hours        │ Total: 10-15 minutes    │
│ Error Risk: HIGH ❌     │ Error Risk: LOW ✓       │
└─────────────────────────┴─────────────────────────┘
```

---

## Summary

### Key Architectural Principles

1. **Single Source of Truth**: ConfigGen C# code
2. **Separation of Concerns**: ConfigGen authors, EV2 executes
3. **Compile-Time Validation**: Catch errors early
4. **Region Agnostic**: One definition for all regions
5. **Type Safety**: Compiler ensures correctness

### Flow Summary

```
Developer → ConfigGen C# → Compile → Generate → EV2 Artifacts → EV2 Deploy → Azure
            (Edit)        (Validate) (Auto)     (JSON)         (Orchestrate) (Resources)
```

### Time Savings

| Operation | Manual EV2 | ConfigGen | Savings |
|-----------|-----------|-----------|---------|
| Add region | 2-3 hours | 15 min | **83%** |
| Update config | 1 hour | 5 min | **92%** |
| Fix bug | 1-2 hours | 10 min | **92%** |
| New environment | 4-6 hours | 1-2 hours | **70%** |

---

For implementation details, see:
- [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) - Step-by-step migration
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Common scenarios
- [PIPELINE_EXAMPLES.md](PIPELINE_EXAMPLES.md) - Pipeline configurations

*Last Updated: 2026-02-18*
