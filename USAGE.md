# Ev2 to Cgv2 Converter - Usage Guide

## Table of Contents
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Command Line Usage](#command-line-usage)
- [Python API](#python-api)
- [Examples](#examples)
- [Architecture](#architecture)

## Installation

### Prerequisites
- Python 3.7 or higher
- pip package manager

### Install from source
```bash
git clone https://github.com/factorygithub/Ev2toCgv2.git
cd Ev2toCgv2
pip install -r requirements.txt
pip install -e .
```

## Quick Start

Convert an Ev2 JSON file to Cgv2 format:
```bash
ev2-to-cgv2 input.json output.json
```

Convert to YAML format:
```bash
ev2-to-cgv2 input.json output.yaml --format yaml
```

## Command Line Usage

### Basic Syntax
```bash
ev2-to-cgv2 INPUT OUTPUT [OPTIONS]
```

### Options
- `INPUT`: Path to input Ev2 file (JSON or YAML)
- `OUTPUT`: Path to output Cgv2 file
- `-f, --format {json,yaml}`: Output format (default: json)
- `-v, --verbose`: Enable verbose output
- `-h, --help`: Show help message

### Examples

#### Convert JSON to JSON
```bash
ev2-to-cgv2 webapp.json webapp-cgv2.json
```

#### Convert YAML to YAML
```bash
ev2-to-cgv2 storage.yaml storage-cgv2.yaml --format yaml
```

#### Convert with verbose output
```bash
ev2-to-cgv2 deployment.json output.json --verbose
```

## Python API

### Basic Usage

```python
from converter.converter import Ev2ToCgv2Converter

# Create converter instance
converter = Ev2ToCgv2Converter()

# Convert a file
converter.convert_file('input.json', 'output.json', format='json')
```

### Convert data directly

```python
from converter.converter import Ev2ToCgv2Converter

# Your Ev2 specification
ev2_data = {
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
    "parameters": {
        "location": {
            "type": "string",
            "defaultValue": "westus2"
        }
    },
    "resources": [
        {
            "type": "Microsoft.Storage/storageAccounts",
            "name": "mystorageaccount",
            "apiVersion": "2021-09-01",
            "location": "[parameters('location')]"
        }
    ]
}

# Convert
converter = Ev2ToCgv2Converter()
cgv2_spec = converter.convert(ev2_data)

# Access the result
print(cgv2_spec['apiVersion'])  # "cgv2/v1"
print(cgv2_spec['kind'])  # "DeploymentSpecification"
```

### Using Individual Components

#### Parse Ev2 Files
```python
from converter.ev2_parser import Ev2Parser

parser = Ev2Parser()
ev2_data = parser.parse_file('input.json')

# Extract components
resources = parser.extract_resources()
parameters = parser.extract_parameters()
variables = parser.extract_variables()
rollout_spec = parser.extract_rollout_spec()
```

#### Generate Cgv2 Files
```python
from converter.cgv2_generator import Cgv2Generator

generator = Cgv2Generator()

# Set metadata
generator.set_metadata(
    name="my-deployment",
    description="My deployment",
    labels={"env": "prod"}
)

# Add parameters
generator.add_parameter(
    name="location",
    param_type="string",
    default_value="westus2",
    description="Resource location"
)

# Add resources
generator.add_resource({
    "name": "myStorage",
    "type": "Microsoft.Storage/storageAccounts",
    "location": "westus2",
    "properties": {
        "accountType": "Standard_LRS"
    }
})

# Set deployment strategy
generator.set_deployment_strategy(
    strategy="rolling",
    regions=["westus2", "eastus2"],
    health_check={"endpoint": "/health", "interval": 30}
)

# Save to file
generator.save_to_file('output.json', format='json')
```

## Examples

### Example 1: Simple Storage Account Conversion

**Input (Ev2 - sample-storage.yaml):**
```yaml
$schema: "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"
parameters:
  storageAccountName:
    type: string
  location:
    type: string
    defaultValue: "westus2"

resources:
  - type: "Microsoft.Storage/storageAccounts"
    apiVersion: "2021-09-01"
    name: "[parameters('storageAccountName')]"
    location: "[parameters('location')]"
    sku:
      name: "Standard_LRS"
    kind: "StorageV2"
```

**Output (Cgv2):**
```yaml
apiVersion: cgv2/v1
kind: DeploymentSpecification
metadata:
  name: ev2-deployment
spec:
  parameters:
    storageAccountName:
      type: string
    location:
      type: string
      default: westus2
  resources:
    - name: "[parameters('storageAccountName')]"
      type: Microsoft.Storage/storageAccounts
      location: "[parameters('location')]"
      apiVersion: "2021-09-01"
  deployment:
    strategy: rolling
```

### Example 2: Web Application with Rollout Spec

See `examples/ev2/sample-webapp.json` for a complete example with:
- Multiple resources
- Complex parameters
- Variables
- Rollout specification with health checks
- Tags and dependencies

## Architecture

### Components

1. **Ev2Parser** (`src/converter/ev2_parser.py`)
   - Parses Ev2 JSON and YAML files
   - Extracts parameters, variables, resources
   - Handles ARM template extensions

2. **Cgv2Generator** (`src/converter/cgv2_generator.py`)
   - Generates Cgv2 specifications
   - Supports JSON and YAML output
   - Manages metadata, parameters, resources, and deployment config

3. **Ev2ToCgv2Converter** (`src/converter/converter.py`)
   - Main conversion logic
   - Orchestrates parsing and generation
   - Transforms Ev2 concepts to Cgv2 equivalents

4. **CLI** (`src/converter/cli.py`)
   - Command-line interface
   - Argument parsing
   - Error handling

### Conversion Mapping

| Ev2 Concept | Cgv2 Concept |
|------------|--------------|
| parameters | spec.parameters |
| variables | spec.variables |
| resources | spec.resources |
| rolloutSpec | spec.deployment |
| rolloutSpec.targetRegions | deployment.regions |
| rolloutSpec.healthCheck | deployment.healthCheck |
| rolloutSpec.strategy | deployment.strategy |

## Testing

Run the test suite:
```bash
# Run all tests
python -m unittest discover tests

# Run specific test module
python -m unittest tests.test_converter

# Run with verbose output
python -m unittest discover tests -v
```

## Troubleshooting

### Import Errors
If you get `ModuleNotFoundError: No module named 'converter'`, install the package:
```bash
pip install -e .
```

### File Not Found Errors
Make sure the input file path is correct and the file exists:
```bash
ls -la path/to/input.json
```

### Parse Errors
Ensure your Ev2 file is valid JSON or YAML:
```bash
# Validate JSON
python -m json.tool input.json

# Validate YAML
python -c "import yaml; yaml.safe_load(open('input.yaml'))"
```

## Best Practices

1. **Validate Input**: Ensure your Ev2 files are well-formed before conversion
2. **Review Output**: Always review the generated Cgv2 files for accuracy
3. **Test Deployments**: Test converted specifications in a non-production environment first
4. **Version Control**: Keep both Ev2 and Cgv2 files in version control during migration
5. **Incremental Migration**: Convert and test one deployment at a time

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Review existing examples in the `examples/` directory
- Check test cases in `tests/` for usage patterns
