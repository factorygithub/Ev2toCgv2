"""
Ev2 Parser Module

Parses Azure Express V2 (Ev2) deployment specification files.
Ev2 files typically contain ARM templates with Ev2-specific extensions.
"""

import json
import yaml
from pathlib import Path
from typing import Dict, Any, List, Optional


class Ev2Parser:
    """Parser for Ev2 deployment specification files."""

    def __init__(self):
        """Initialize the Ev2 parser."""
        self.parsed_data = None

    def parse_file(self, file_path: str) -> Dict[str, Any]:
        """
        Parse an Ev2 specification file.

        Args:
            file_path: Path to the Ev2 file (JSON or YAML)

        Returns:
            Dictionary containing parsed Ev2 specification

        Raises:
            FileNotFoundError: If file doesn't exist
            ValueError: If file format is invalid
        """
        path = Path(file_path)
        
        if not path.exists():
            raise FileNotFoundError(f"File not found: {file_path}")

        try:
            with open(path, 'r') as f:
                if path.suffix.lower() in ['.json']:
                    self.parsed_data = json.load(f)
                elif path.suffix.lower() in ['.yaml', '.yml']:
                    self.parsed_data = yaml.safe_load(f)
                else:
                    # Try JSON first, then YAML
                    content = f.read()
                    try:
                        self.parsed_data = json.loads(content)
                    except json.JSONDecodeError:
                        self.parsed_data = yaml.safe_load(content)
        except Exception as e:
            raise ValueError(f"Failed to parse file {file_path}: {str(e)}")

        return self.parsed_data

    def extract_resources(self) -> List[Dict[str, Any]]:
        """
        Extract resources from the parsed Ev2 specification.

        Returns:
            List of resource definitions
        """
        if not self.parsed_data:
            return []

        # Ev2 files often extend ARM templates
        resources = []
        
        # Check for ARM template structure
        if isinstance(self.parsed_data, dict):
            if 'resources' in self.parsed_data:
                resources = self.parsed_data['resources']
            elif 'Resources' in self.parsed_data:
                resources = self.parsed_data['Resources']

        return resources if isinstance(resources, list) else []

    def extract_parameters(self) -> Dict[str, Any]:
        """
        Extract parameters from the parsed Ev2 specification.

        Returns:
            Dictionary of parameters
        """
        if not self.parsed_data or not isinstance(self.parsed_data, dict):
            return {}

        return self.parsed_data.get('parameters', self.parsed_data.get('Parameters', {}))

    def extract_variables(self) -> Dict[str, Any]:
        """
        Extract variables from the parsed Ev2 specification.

        Returns:
            Dictionary of variables
        """
        if not self.parsed_data or not isinstance(self.parsed_data, dict):
            return {}

        return self.parsed_data.get('variables', self.parsed_data.get('Variables', {}))

    def extract_rollout_spec(self) -> Optional[Dict[str, Any]]:
        """
        Extract Ev2-specific rollout specification if present.

        Returns:
            Rollout specification dictionary or None
        """
        if not self.parsed_data or not isinstance(self.parsed_data, dict):
            return None

        # Look for Ev2-specific rollout configuration
        for key in ['rolloutSpec', 'RolloutSpec', 'rolloutSpecification']:
            if key in self.parsed_data:
                return self.parsed_data[key]

        return None

    def get_schema_version(self) -> Optional[str]:
        """
        Get the schema version from the Ev2 specification.

        Returns:
            Schema version string or None
        """
        if not self.parsed_data or not isinstance(self.parsed_data, dict):
            return None

        return self.parsed_data.get('$schema', self.parsed_data.get('schema'))
