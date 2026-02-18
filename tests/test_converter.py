"""
Integration tests for Ev2 to Cgv2 Converter
"""

import unittest
import tempfile
import json
from pathlib import Path
from converter.converter import Ev2ToCgv2Converter


class TestEv2ToCgv2Converter(unittest.TestCase):
    """Test cases for Ev2ToCgv2Converter class."""

    def setUp(self):
        """Set up test fixtures."""
        self.converter = Ev2ToCgv2Converter()
        self.sample_ev2_data = {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "name": "test-deployment",
            "description": "Test deployment",
            "parameters": {
                "location": {
                    "type": "string",
                    "defaultValue": "westus2",
                    "metadata": {
                        "description": "Resource location"
                    }
                }
            },
            "variables": {
                "storageAccountName": "mystorageaccount"
            },
            "resources": [
                {
                    "type": "Microsoft.Storage/storageAccounts",
                    "name": "testStorage",
                    "apiVersion": "2021-09-01",
                    "location": "[parameters('location')]",
                    "properties": {
                        "accountType": "Standard_LRS"
                    }
                }
            ],
            "rolloutSpec": {
                "strategy": "rolling",
                "targetRegions": ["westus2", "eastus2"],
                "healthCheck": {
                    "endpoint": "/health",
                    "interval": 30
                }
            }
        }

    def test_convert(self):
        """Test basic conversion."""
        result = self.converter.convert(self.sample_ev2_data)
        
        # Check structure
        self.assertIn('apiVersion', result)
        self.assertIn('kind', result)
        self.assertIn('metadata', result)
        self.assertIn('spec', result)
        
        # Check metadata
        self.assertEqual(result['metadata']['name'], 'test-deployment')
        
        # Check parameters
        self.assertIn('location', result['spec']['parameters'])
        
        # Check variables
        self.assertIn('storageAccountName', result['spec']['variables'])
        
        # Check resources
        self.assertEqual(len(result['spec']['resources']), 1)
        
        # Check deployment strategy
        deployment = result['spec']['deployment']
        self.assertEqual(deployment['strategy'], 'rolling')
        self.assertEqual(len(deployment['regions']), 2)

    def test_convert_file(self):
        """Test file conversion."""
        # Create temporary input file
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump(self.sample_ev2_data, f)
            input_path = f.name

        # Create temporary output path
        output_path = tempfile.mktemp(suffix='.json')

        try:
            result = self.converter.convert_file(input_path, output_path, 'json')
            
            # Check output file exists
            self.assertTrue(Path(output_path).exists())
            
            # Check content
            with open(output_path, 'r') as f:
                saved_data = json.load(f)
            
            self.assertEqual(saved_data['apiVersion'], 'cgv2/v1')
            self.assertEqual(saved_data['metadata']['name'], 'test-deployment')
            
        finally:
            Path(input_path).unlink()
            if Path(output_path).exists():
                Path(output_path).unlink()

    def test_convert_without_rollout_spec(self):
        """Test conversion without rollout specification."""
        data_without_rollout = {
            **self.sample_ev2_data
        }
        del data_without_rollout['rolloutSpec']
        
        result = self.converter.convert(data_without_rollout)
        
        # Should have default deployment strategy
        self.assertIn('deployment', result['spec'])
        self.assertEqual(result['spec']['deployment']['strategy'], 'rolling')

    def test_convert_minimal_ev2(self):
        """Test conversion with minimal Ev2 data."""
        minimal_data = {
            "resources": []
        }
        
        result = self.converter.convert(minimal_data)
        
        # Should create valid Cgv2 spec
        self.assertEqual(result['apiVersion'], 'cgv2/v1')
        self.assertIn('metadata', result)
        self.assertIn('spec', result)


if __name__ == '__main__':
    unittest.main()
