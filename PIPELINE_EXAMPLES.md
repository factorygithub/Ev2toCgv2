# Pipeline Configuration Examples

This document provides sample Azure DevOps pipeline configurations for both the old (manual EV2) and new (ConfigGen-based) approaches.

## Table of Contents
- [Before: Manual EV2 Pipeline](#before-manual-ev2-pipeline)
- [After: ConfigGen Pipeline](#after-configgen-pipeline)
- [Environment Variables](#environment-variables)
- [Service Connections](#service-connections)

---

## Before: Manual EV2 Pipeline

### Legacy YAML Pipeline (azure-pipelines.old.yml)

```yaml
# Old approach: Manual EV2 artifact generation
trigger:
  branches:
    include:
    - main
  paths:
    include:
    - ServiceGroupRoot/*

pool:
  vmImage: 'ubuntu-latest'

variables:
  - group: EV2-Production
  - name: serviceGroupRoot
    value: '$(Build.SourcesDirectory)/ServiceGroupRoot'

stages:
- stage: Build
  displayName: 'Build and Prepare EV2 Artifacts'
  jobs:
  - job: PrepareArtifacts
    displayName: 'Prepare EV2 Artifacts'
    steps:
    - task: PowerShell@2
      displayName: 'Substitute Parameters - East US'
      inputs:
        targetType: 'inline'
        script: |
          # Manual parameter substitution
          $params = Get-Content 'Parameters/storage.eastus.parameters.json' | ConvertFrom-Json
          $params.parameters.location.value = "eastus"
          $params.parameters.sku.value = "Standard_GRS"
          $params | ConvertTo-Json -Depth 10 | Set-Content 'Parameters/storage.eastus.parameters.json'
          
    - task: PowerShell@2
      displayName: 'Substitute Parameters - West US 2'
      inputs:
        targetType: 'inline'
        script: |
          $params = Get-Content 'Parameters/storage.westus2.parameters.json' | ConvertFrom-Json
          $params.parameters.location.value = "westus2"
          $params.parameters.sku.value = "Standard_GRS"
          $params | ConvertTo-Json -Depth 10 | Set-Content 'Parameters/storage.westus2.parameters.json'
    
    - task: CopyFiles@2
      displayName: 'Copy EV2 Artifacts'
      inputs:
        SourceFolder: '$(serviceGroupRoot)'
        Contents: |
          ServiceModel.json
          RolloutSpec.json
          Templates/**
          Parameters/**
        TargetFolder: '$(Build.ArtifactStagingDirectory)/ServiceGroupRoot'
    
    - task: PublishBuildArtifacts@1
      displayName: 'Publish EV2 Artifacts'
      inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)/ServiceGroupRoot'
        ArtifactName: 'ev2-artifacts'

- stage: Deploy_Test
  displayName: 'Deploy to Test'
  dependsOn: Build
  condition: succeeded()
  jobs:
  - deployment: DeployTest
    displayName: 'Deploy Test Environment'
    environment: 'Test'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: EV2@1
            displayName: 'Deploy with EV2 to Test'
            inputs:
              connectionType: 'ConnectedService'
              connectedServiceName: 'EV2-Test-Connection'
              serviceGroupRoot: '$(Pipeline.Workspace)/ev2-artifacts'
              rolloutSpecPath: '$(Pipeline.Workspace)/ev2-artifacts/RolloutSpec.json'

- stage: Deploy_Production
  displayName: 'Deploy to Production'
  dependsOn: Deploy_Test
  condition: succeeded()
  jobs:
  - deployment: DeployProduction
    displayName: 'Deploy Production Environment'
    environment: 'Production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: EV2@1
            displayName: 'Deploy with EV2 to Production'
            inputs:
              connectionType: 'ConnectedService'
              connectedServiceName: 'EV2-Prod-Connection'
              serviceGroupRoot: '$(Pipeline.Workspace)/ev2-artifacts'
              rolloutSpecPath: '$(Pipeline.Workspace)/ev2-artifacts/RolloutSpec.json'
```

**Problems with this approach:**
- ❌ Manual parameter substitution (error-prone)
- ❌ Duplicate files per region
- ❌ No compile-time validation
- ❌ Hard to maintain consistency
- ❌ Difficult to add new regions

---

## After: ConfigGen Pipeline

### Modern YAML Pipeline (azure-pipelines.yml)

```yaml
# New approach: ConfigGen-based EV2 artifact generation
trigger:
  branches:
    include:
    - main
  paths:
    include:
    - ConfigurationGeneration/**

pool:
  vmImage: 'ubuntu-latest'

variables:
  - group: EV2-Production
  - name: configGenProject
    value: '$(Build.SourcesDirectory)/ConfigurationGeneration/ConfigurationGeneration.csproj'
  - name: dotnetVersion
    value: '6.x'

stages:
- stage: Build
  displayName: 'Build ConfigGen and Generate EV2 Artifacts'
  jobs:
  - job: BuildConfigGen
    displayName: 'Build ConfigGen Project'
    steps:
    # Install .NET SDK
    - task: UseDotNetCore@2
      displayName: 'Install .NET SDK'
      inputs:
        version: $(dotnetVersion)
        
    # Restore NuGet packages
    - task: DotNetCoreCLI@2
      displayName: 'Restore NuGet Packages'
      inputs:
        command: 'restore'
        projects: '$(configGenProject)'
        
    # Build ConfigGen project
    - task: DotNetCoreCLI@2
      displayName: 'Build ConfigGen Project'
      inputs:
        command: 'build'
        projects: '$(configGenProject)'
        arguments: '--configuration Release'
    
    # Run unit tests
    - task: DotNetCoreCLI@2
      displayName: 'Run ConfigGen Tests'
      inputs:
        command: 'test'
        projects: '**/Tests/*.csproj'
        arguments: '--configuration Release --collect:"XPlat Code Coverage"'
    
    # Generate EV2 artifacts for Test environment
    - task: DotNetCoreCLI@2
      displayName: 'Generate EV2 Artifacts - Test'
      inputs:
        command: 'run'
        projects: '$(configGenProject)'
        arguments: '--environment Test --output $(Build.ArtifactStagingDirectory)/Test'
    
    # Generate EV2 artifacts for Production environment
    - task: DotNetCoreCLI@2
      displayName: 'Generate EV2 Artifacts - Production'
      inputs:
        command: 'run'
        projects: '$(configGenProject)'
        arguments: '--environment Production --output $(Build.ArtifactStagingDirectory)/Production'
    
    # Validate generated EV2 artifacts
    - task: PowerShell@2
      displayName: 'Validate EV2 Artifacts'
      inputs:
        targetType: 'inline'
        script: |
          # Validate that ServiceModel.json exists and is valid JSON
          $testServiceModel = "$(Build.ArtifactStagingDirectory)/Test/ServiceModel.json"
          $prodServiceModel = "$(Build.ArtifactStagingDirectory)/Production/ServiceModel.json"
          
          if (!(Test-Path $testServiceModel)) {
            Write-Error "Test ServiceModel.json not found"
            exit 1
          }
          
          if (!(Test-Path $prodServiceModel)) {
            Write-Error "Production ServiceModel.json not found"
            exit 1
          }
          
          # Validate JSON syntax
          try {
            Get-Content $testServiceModel | ConvertFrom-Json | Out-Null
            Get-Content $prodServiceModel | ConvertFrom-Json | Out-Null
            Write-Host "✓ All EV2 artifacts are valid"
          } catch {
            Write-Error "Invalid JSON in generated artifacts: $_"
            exit 1
          }
    
    # Publish Test artifacts
    - task: PublishBuildArtifacts@1
      displayName: 'Publish Test EV2 Artifacts'
      inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)/Test'
        ArtifactName: 'ev2-test-artifacts'
    
    # Publish Production artifacts
    - task: PublishBuildArtifacts@1
      displayName: 'Publish Production EV2 Artifacts'
      inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)/Production'
        ArtifactName: 'ev2-prod-artifacts'

- stage: Deploy_Test
  displayName: 'Deploy to Test'
  dependsOn: Build
  condition: succeeded()
  jobs:
  - deployment: DeployTest
    displayName: 'Deploy Test Environment'
    environment: 'Test'
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: ev2-test-artifacts
            
          - task: EV2@1
            displayName: 'Deploy with EV2 to Test'
            inputs:
              connectionType: 'ConnectedService'
              connectedServiceName: 'EV2-Test-Connection'
              serviceGroupRoot: '$(Pipeline.Workspace)/ev2-test-artifacts'
              rolloutSpecPath: '$(Pipeline.Workspace)/ev2-test-artifacts/RolloutSpec.json'

- stage: Deploy_PPE
  displayName: 'Deploy to PPE'
  dependsOn: Deploy_Test
  condition: succeeded()
  jobs:
  - deployment: DeployPPE
    displayName: 'Deploy PPE Environment'
    environment: 'PPE'
    strategy:
      runOnce:
        deploy:
          steps:
          # PPE uses Production artifacts with PPE-specific overrides
          - download: current
            artifact: ev2-prod-artifacts
            
          - task: EV2@1
            displayName: 'Deploy with EV2 to PPE'
            inputs:
              connectionType: 'ConnectedService'
              connectedServiceName: 'EV2-PPE-Connection'
              serviceGroupRoot: '$(Pipeline.Workspace)/ev2-prod-artifacts'
              rolloutSpecPath: '$(Pipeline.Workspace)/ev2-prod-artifacts/RolloutSpec.json'

- stage: Deploy_Production
  displayName: 'Deploy to Production'
  dependsOn: Deploy_PPE
  condition: succeeded()
  jobs:
  - deployment: DeployProduction
    displayName: 'Deploy Production Environment'
    environment: 'Production'
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: ev2-prod-artifacts
            
          - task: EV2@1
            displayName: 'Deploy with EV2 to Production'
            inputs:
              connectionType: 'ConnectedService'
              connectedServiceName: 'EV2-Prod-Connection'
              serviceGroupRoot: '$(Pipeline.Workspace)/ev2-prod-artifacts'
              rolloutSpecPath: '$(Pipeline.Workspace)/ev2-prod-artifacts/RolloutSpec.json'
```

**Benefits of this approach:**
- ✅ Automatic artifact generation
- ✅ Compile-time validation
- ✅ Type-safe configuration
- ✅ Single source of truth
- ✅ Easy to add new regions
- ✅ Consistent across environments

---

## Environment Variables

### Variable Groups

Create these variable groups in Azure DevOps:

#### EV2-Production
```yaml
variables:
  - name: ServiceName
    value: 'MyAzureService'
  - name: TenantId
    value: '12345678-1234-1234-1234-123456789012'
  - name: SubscriptionId
    value: '87654321-4321-4321-4321-210987654321'
  - name: KeyVaultName
    value: 'myservice-keyvault'
```

#### EV2-Test
```yaml
variables:
  - name: ServiceName
    value: 'MyAzureService-Test'
  - name: TenantId
    value: '12345678-1234-1234-1234-123456789012'
  - name: SubscriptionId
    value: 'test-subscription-id'
```

---

## Service Connections

### Required Service Connections

1. **EV2-Test-Connection**
   - Type: Azure Resource Manager
   - Scope: Subscription
   - Used for: Test environment deployments

2. **EV2-PPE-Connection**
   - Type: Azure Resource Manager
   - Scope: Subscription
   - Used for: PPE environment deployments

3. **EV2-Prod-Connection**
   - Type: Azure Resource Manager
   - Scope: Subscription
   - Used for: Production environment deployments

### Creating Service Connections

```bash
# Azure CLI example for creating service principal
az ad sp create-for-rbac \
  --name "EV2-Prod-SP" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}
```

---

## Advanced Pipeline Configurations

### Multi-Stage with Approvals

```yaml
- stage: Deploy_Production
  displayName: 'Deploy to Production'
  dependsOn: Deploy_PPE
  condition: succeeded()
  jobs:
  - deployment: DeployProduction
    displayName: 'Deploy Production Environment'
    environment: 'Production'  # Environment with approval gates
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: ev2-prod-artifacts
            
          # Manual approval will be required before this step
          - task: EV2@1
            displayName: 'Deploy with EV2 to Production'
            inputs:
              connectionType: 'ConnectedService'
              connectedServiceName: 'EV2-Prod-Connection'
              serviceGroupRoot: '$(Pipeline.Workspace)/ev2-prod-artifacts'
              rolloutSpecPath: '$(Pipeline.Workspace)/ev2-prod-artifacts/RolloutSpec.json'
```

### Conditional Region Deployment

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Generate EV2 Artifacts - Italy North Only'
  condition: eq(variables['DeployToItalyNorth'], 'true')
  inputs:
    command: 'run'
    projects: '$(configGenProject)'
    arguments: '--environment Production --region italynorth --output $(Build.ArtifactStagingDirectory)/ItalyNorth'
```

### Parallel Region Deployment

```yaml
- stage: Deploy_Production
  jobs:
  - deployment: DeployEastUS
    environment: 'Production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: EV2@1
            inputs:
              region: 'eastus'
  
  - deployment: DeployWestUS
    environment: 'Production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: EV2@1
            inputs:
              region: 'westus2'
```

---

## Summary

### Time to Deploy

| Approach | Initial Setup | Per Region | New Environment |
|----------|--------------|------------|-----------------|
| **Manual EV2** | 4-8 hours | 2-3 hours | 4-6 hours |
| **ConfigGen** | 2-4 hours | 30 minutes | 1-2 hours |

### Maintenance Burden

| Task | Manual EV2 | ConfigGen |
|------|-----------|-----------|
| Add new region | Edit 5-10 JSON files | Add 1 DataCenter config |
| Update SKU globally | Edit all parameter files | Change 1 line of code |
| Fix configuration bug | Find/replace across files | Fix in one place |
| Validate changes | Manual testing | Compile-time checks |

---

*For complete migration guide, see `MIGRATION_GUIDE.md`*
