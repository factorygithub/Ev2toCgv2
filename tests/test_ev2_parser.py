"""
Unit tests for Ev2 Parser
"""

import unittest
import json
import tempfile
from pathlib import Path
from converter.ev2_parser import Ev2Parser


class TestEv2Parser(unittest.TestCase):
    """Test cases for Ev2Parser class."""

    def setUp(self):
        """Set up test fixtures."""
        self.parser = Ev2Parser()
        self.sample_ev2_data = {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "parameters": {
                "location": {
                    "type": "string",
                    "defaultValue": "westus2"
                }
            },
            "variables": {
                "storageAccountName": "mystorageaccount"
            },
            "resources": [
                {
                    "type": "Microsoft.Storage/storageAccounts",
                    "name": "testStorage",
                    "apiVersion": "2021-09-01"
                }
            ]
        }

    def test_parse_json_file(self):
        """Test parsing a JSON file."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump(self.sample_ev2_data, f)
            temp_path = f.name

        try:
            result = self.parser.parse_file(temp_path)
            self.assertIsNotNone(result)
            self.assertEqual(result['$schema'], self.sample_ev2_data['$schema'])
        finally:
            Path(temp_path).unlink()

    def test_parse_nonexistent_file(self):
        """Test parsing a nonexistent file raises FileNotFoundError."""
        with self.assertRaises(FileNotFoundError):
            self.parser.parse_file('/nonexistent/file.json')

    def test_extract_resources(self):
        """Test extracting resources from parsed data."""
        self.parser.parsed_data = self.sample_ev2_data
        resources = self.parser.extract_resources()
        self.assertEqual(len(resources), 1)
        self.assertEqual(resources[0]['type'], 'Microsoft.Storage/storageAccounts')

    def test_extract_parameters(self):
        """Test extracting parameters from parsed data."""
        self.parser.parsed_data = self.sample_ev2_data
        parameters = self.parser.extract_parameters()
        self.assertIn('location', parameters)
        self.assertEqual(parameters['location']['type'], 'string')

    def test_extract_variables(self):
        """Test extracting variables from parsed data."""
        self.parser.parsed_data = self.sample_ev2_data
        variables = self.parser.extract_variables()
        self.assertIn('storageAccountName', variables)
        self.assertEqual(variables['storageAccountName'], 'mystorageaccount')

    def test_get_schema_version(self):
        """Test getting schema version."""
        self.parser.parsed_data = self.sample_ev2_data
        schema = self.parser.get_schema_version()
        self.assertIsNotNone(schema)
        self.assertIn('deploymentTemplate.json', schema)

    def test_extract_rollout_spec(self):
        """Test extracting rollout specification."""
        data_with_rollout = {
            **self.sample_ev2_data,
            "rolloutSpec": {
                "strategy": "rolling",
                "targetRegions": ["westus2"]
            }
        }
        self.parser.parsed_data = data_with_rollout
        rollout_spec = self.parser.extract_rollout_spec()
        self.assertIsNotNone(rollout_spec)
        self.assertEqual(rollout_spec['strategy'], 'rolling')


if __name__ == '__main__':
    unittest.main()
