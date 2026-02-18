"""
Cgv2 Generator Module

Generates Cloud Generation V2 (Cgv2) deployment specification files.
"""

import json
import yaml
from pathlib import Path
from typing import Dict, Any, List, Optional
from datetime import datetime, timezone


class Cgv2Generator:
    """Generator for Cgv2 deployment specification files."""

    def __init__(self):
        """Initialize the Cgv2 generator."""
        self.spec = {
            "apiVersion": "cgv2/v1",
            "kind": "DeploymentSpecification",
            "metadata": {
                "name": "",
                "createdAt": datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')
            },
            "spec": {
                "parameters": {},
                "variables": {},
                "resources": [],
                "deployment": {}
            }
        }

    def set_metadata(self, name: str, description: Optional[str] = None,
                    labels: Optional[Dict[str, str]] = None):
        """
        Set metadata for the Cgv2 specification.

        Args:
            name: Name of the deployment
            description: Optional description
            labels: Optional labels dictionary
        """
        self.spec["metadata"]["name"] = name
        if description:
            self.spec["metadata"]["description"] = description
        if labels:
            self.spec["metadata"]["labels"] = labels

    def add_parameter(self, name: str, param_type: str, 
                     default_value: Any = None, description: str = ""):
        """
        Add a parameter to the Cgv2 specification.

        Args:
            name: Parameter name
            param_type: Parameter type (string, int, bool, etc.)
            default_value: Optional default value
            description: Parameter description
        """
        param_def = {
            "type": param_type,
            "description": description
        }
        if default_value is not None:
            param_def["default"] = default_value

        self.spec["spec"]["parameters"][name] = param_def

    def add_variable(self, name: str, value: Any):
        """
        Add a variable to the Cgv2 specification.

        Args:
            name: Variable name
            value: Variable value
        """
        self.spec["spec"]["variables"][name] = value

    def add_resource(self, resource: Dict[str, Any]):
        """
        Add a resource to the Cgv2 specification.

        Args:
            resource: Resource definition dictionary
        """
        # Convert ARM-style resource to Cgv2 format
        cgv2_resource = {
            "name": resource.get("name", ""),
            "type": resource.get("type", ""),
            "properties": resource.get("properties", {}),
        }

        # Add optional fields
        if "location" in resource:
            cgv2_resource["location"] = resource["location"]
        if "tags" in resource:
            cgv2_resource["tags"] = resource["tags"]
        if "dependsOn" in resource:
            cgv2_resource["dependsOn"] = resource["dependsOn"]
        if "apiVersion" in resource:
            cgv2_resource["apiVersion"] = resource["apiVersion"]

        self.spec["spec"]["resources"].append(cgv2_resource)

    def set_deployment_strategy(self, strategy: str = "rolling",
                                regions: Optional[List[str]] = None,
                                health_check: Optional[Dict[str, Any]] = None):
        """
        Set deployment strategy for the Cgv2 specification.

        Args:
            strategy: Deployment strategy (rolling, blue-green, canary)
            regions: List of target regions
            health_check: Health check configuration
        """
        deployment_config = {
            "strategy": strategy
        }

        if regions:
            deployment_config["regions"] = regions

        if health_check:
            deployment_config["healthCheck"] = health_check

        self.spec["spec"]["deployment"] = deployment_config

    def to_dict(self) -> Dict[str, Any]:
        """
        Get the Cgv2 specification as a dictionary.

        Returns:
            Dictionary representation of the Cgv2 spec
        """
        return self.spec

    def to_json(self, indent: int = 2) -> str:
        """
        Convert the Cgv2 specification to JSON string.

        Args:
            indent: JSON indentation level

        Returns:
            JSON string
        """
        return json.dumps(self.spec, indent=indent)

    def to_yaml(self) -> str:
        """
        Convert the Cgv2 specification to YAML string.

        Returns:
            YAML string
        """
        return yaml.dump(self.spec, default_flow_style=False, sort_keys=False)

    def save_to_file(self, file_path: str, format: str = "json"):
        """
        Save the Cgv2 specification to a file.

        Args:
            file_path: Output file path
            format: Output format ('json' or 'yaml')

        Raises:
            ValueError: If format is not supported
        """
        path = Path(file_path)
        path.parent.mkdir(parents=True, exist_ok=True)

        with open(path, 'w') as f:
            if format.lower() == "json":
                f.write(self.to_json())
            elif format.lower() == "yaml":
                f.write(self.to_yaml())
            else:
                raise ValueError(f"Unsupported format: {format}")
