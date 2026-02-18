"""
Unit tests for Cgv2 Generator
"""

import unittest
import json
from converter.cgv2_generator import Cgv2Generator


class TestCgv2Generator(unittest.TestCase):
    """Test cases for Cgv2Generator class."""

    def setUp(self):
        """Set up test fixtures."""
        self.generator = Cgv2Generator()

    def test_initialization(self):
        """Test generator initialization."""
        spec = self.generator.to_dict()
        self.assertEqual(spec['apiVersion'], 'cgv2/v1')
        self.assertEqual(spec['kind'], 'DeploymentSpecification')
        self.assertIn('metadata', spec)
        self.assertIn('spec', spec)

    def test_set_metadata(self):
        """Test setting metadata."""
        self.generator.set_metadata(
            name="test-deployment",
            description="Test description",
            labels={"env": "prod"}
        )
        spec = self.generator.to_dict()
        self.assertEqual(spec['metadata']['name'], 'test-deployment')
        self.assertEqual(spec['metadata']['description'], 'Test description')
        self.assertEqual(spec['metadata']['labels']['env'], 'prod')

    def test_add_parameter(self):
        """Test adding parameters."""
        self.generator.add_parameter(
            name="location",
            param_type="string",
            default_value="westus2",
            description="Resource location"
        )
        spec = self.generator.to_dict()
        self.assertIn('location', spec['spec']['parameters'])
        param = spec['spec']['parameters']['location']
        self.assertEqual(param['type'], 'string')
        self.assertEqual(param['default'], 'westus2')
        self.assertEqual(param['description'], 'Resource location')

    def test_add_variable(self):
        """Test adding variables."""
        self.generator.add_variable("accountName", "myaccount123")
        spec = self.generator.to_dict()
        self.assertIn('accountName', spec['spec']['variables'])
        self.assertEqual(spec['spec']['variables']['accountName'], 'myaccount123')

    def test_add_resource(self):
        """Test adding resources."""
        resource = {
            "name": "myStorage",
            "type": "Microsoft.Storage/storageAccounts",
            "location": "westus2",
            "properties": {
                "accountType": "Standard_LRS"
            }
        }
        self.generator.add_resource(resource)
        spec = self.generator.to_dict()
        self.assertEqual(len(spec['spec']['resources']), 1)
        added_resource = spec['spec']['resources'][0]
        self.assertEqual(added_resource['name'], 'myStorage')
        self.assertEqual(added_resource['type'], 'Microsoft.Storage/storageAccounts')

    def test_set_deployment_strategy(self):
        """Test setting deployment strategy."""
        self.generator.set_deployment_strategy(
            strategy="rolling",
            regions=["westus2", "eastus2"],
            health_check={"endpoint": "/health", "interval": 30}
        )
        spec = self.generator.to_dict()
        deployment = spec['spec']['deployment']
        self.assertEqual(deployment['strategy'], 'rolling')
        self.assertEqual(len(deployment['regions']), 2)
        self.assertIn('healthCheck', deployment)

    def test_to_json(self):
        """Test JSON conversion."""
        self.generator.set_metadata(name="test")
        json_str = self.generator.to_json()
        self.assertIsInstance(json_str, str)
        parsed = json.loads(json_str)
        self.assertEqual(parsed['metadata']['name'], 'test')

    def test_to_yaml(self):
        """Test YAML conversion."""
        self.generator.set_metadata(name="test")
        yaml_str = self.generator.to_yaml()
        self.assertIsInstance(yaml_str, str)
        self.assertIn('apiVersion', yaml_str)
        self.assertIn('metadata', yaml_str)


if __name__ == '__main__':
    unittest.main()
