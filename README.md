# Ev2 to Cgv2 Converter

A Python tool for converting Azure Express V2 (Ev2) deployment specifications to Cloud Generation V2 (Cgv2) format.

## Overview

This converter helps transform deployment specifications from the older Ev2 (Express V2) format to the newer Cgv2 (Cloud Generation V2) format, facilitating migration and modernization of Azure deployment pipelines.

## Features

- **Multiple Format Support**: Parse both JSON and YAML Ev2 files
- **Comprehensive Conversion**: Converts parameters, variables, resources, and deployment strategies
- **Flexible Output**: Generate Cgv2 specifications in JSON or YAML format
- **CLI Interface**: Easy-to-use command-line tool
- **Well-Tested**: Comprehensive unit and integration tests

## Installation

### From Source

```bash
git clone https://github.com/factorygithub/Ev2toCgv2.git
cd Ev2toCgv2
pip install -r requirements.txt
pip install -e .
```

## Usage

### Command Line

Convert an Ev2 file to Cgv2 format:

```bash
# Convert to JSON (default)
ev2-to-cgv2 examples/ev2/sample-webapp.json output/webapp.json

# Convert to YAML
ev2-to-cgv2 examples/ev2/sample-storage.yaml output/storage.yaml --format yaml

# Verbose output
ev2-to-cgv2 input.json output.json --verbose
```

### Python API

```python
from converter.converter import Ev2ToCgv2Converter

# Create converter instance
converter = Ev2ToCgv2Converter()

# Convert a file
result = converter.convert_file('input.json', 'output.json', format='json')

# Or convert data directly
ev2_data = {...}  # Your Ev2 specification
cgv2_spec = converter.convert(ev2_data)
```

## Conversion Details

### What Gets Converted

- **Parameters**: Ev2 parameter definitions → Cgv2 parameter specifications
- **Variables**: Ev2 variables → Cgv2 variables
- **Resources**: ARM template resources → Cgv2 resource definitions
- **Rollout Specifications**: Ev2 rollout configs → Cgv2 deployment strategies

### Deployment Strategies

The converter supports various deployment strategies:
- `rolling`: Gradual rollout across regions
- `blue-green`: Blue-green deployment
- `canary`: Canary deployment

### Example Transformation

**Input (Ev2)**:
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "parameters": {
    "location": {
      "type": "string",
      "defaultValue": "westus2"
    }
  },
  "resources": [...],
  "rolloutSpec": {
    "strategy": "rolling",
    "targetRegions": ["westus2", "eastus2"]
  }
}
```

**Output (Cgv2)**:
```json
{
  "apiVersion": "cgv2/v1",
  "kind": "DeploymentSpecification",
  "metadata": {
    "name": "deployment-name",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "spec": {
    "parameters": {...},
    "resources": [...],
    "deployment": {
      "strategy": "rolling",
      "regions": ["westus2", "eastus2"]
    }
  }
}
```

## Testing

Run the test suite:

```bash
# Run all tests
python -m pytest tests/

# Run specific test file
python -m pytest tests/test_converter.py

# Run with coverage
python -m pytest --cov=converter tests/
```

Or using unittest:

```bash
python -m unittest discover tests
```

## Project Structure

```
Ev2toCgv2/
├── src/
│   └── converter/
│       ├── __init__.py
│       ├── cli.py              # Command-line interface
│       ├── converter.py        # Main conversion logic
│       ├── ev2_parser.py       # Ev2 file parser
│       └── cgv2_generator.py   # Cgv2 file generator
├── tests/
│   ├── test_ev2_parser.py
│   ├── test_cgv2_generator.py
│   └── test_converter.py
├── examples/
│   ├── ev2/
│   │   ├── sample-webapp.json
│   │   └── sample-storage.yaml
│   └── cgv2/
├── requirements.txt
├── setup.py
└── README.md
```

## Requirements

- Python 3.7+
- PyYAML >= 6.0.1
- jsonschema >= 4.17.0

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is provided as-is for demonstration purposes.

## Support

For issues and questions, please open an issue on GitHub.
