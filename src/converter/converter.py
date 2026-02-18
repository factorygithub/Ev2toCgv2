"""
Converter Module

Main converter logic for transforming Ev2 specifications to Cgv2 format.
"""

from typing import Dict, Any, Optional
from .ev2_parser import Ev2Parser
from .cgv2_generator import Cgv2Generator


class Ev2ToCgv2Converter:
    """Main converter class for Ev2 to Cgv2 transformation."""

    def __init__(self):
        """Initialize the converter."""
        self.parser = Ev2Parser()
        self.generator = Cgv2Generator()

    def convert_file(self, input_path: str, output_path: str,
                    output_format: str = "json") -> Dict[str, Any]:
        """
        Convert an Ev2 file to Cgv2 format.

        Args:
            input_path: Path to input Ev2 file
            output_path: Path to output Cgv2 file
            output_format: Output format ('json' or 'yaml')

        Returns:
            Dictionary containing the converted Cgv2 specification

        Raises:
            FileNotFoundError: If input file doesn't exist
            ValueError: If conversion fails
        """
        # Parse the Ev2 file
        ev2_data = self.parser.parse_file(input_path)

        # Convert to Cgv2
        cgv2_spec = self.convert(ev2_data)

        # Save to file
        self.generator.save_to_file(output_path, output_format)

        return cgv2_spec

    def convert(self, ev2_data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Convert Ev2 specification to Cgv2 format.

        Args:
            ev2_data: Parsed Ev2 specification dictionary

        Returns:
            Cgv2 specification dictionary
        """
        # Set up parser with data
        self.parser.parsed_data = ev2_data

        # Extract components from Ev2
        parameters = self.parser.extract_parameters()
        variables = self.parser.extract_variables()
        resources = self.parser.extract_resources()
        rollout_spec = self.parser.extract_rollout_spec()

        # Set metadata
        deployment_name = self._extract_deployment_name(ev2_data)
        self.generator.set_metadata(
            name=deployment_name,
            description=ev2_data.get("description", "Converted from Ev2")
        )

        # Convert parameters
        self._convert_parameters(parameters)

        # Convert variables
        self._convert_variables(variables)

        # Convert resources
        for resource in resources:
            self.generator.add_resource(resource)

        # Convert rollout specification
        self._convert_rollout_spec(rollout_spec)

        return self.generator.to_dict()

    def _extract_deployment_name(self, ev2_data: Dict[str, Any]) -> str:
        """
        Extract deployment name from Ev2 data.

        Args:
            ev2_data: Ev2 specification dictionary

        Returns:
            Deployment name string
        """
        # Try various common name fields
        for key in ['name', 'deploymentName', 'metadata.name']:
            if '.' in key:
                parts = key.split('.')
                value = ev2_data
                for part in parts:
                    value = value.get(part, {})
                    if not isinstance(value, dict):
                        break
                if value and isinstance(value, str):
                    return value
            elif key in ev2_data:
                return ev2_data[key]

        return "ev2-deployment"

    def _convert_parameters(self, parameters: Dict[str, Any]):
        """
        Convert Ev2 parameters to Cgv2 format.

        Args:
            parameters: Ev2 parameters dictionary
        """
        for param_name, param_def in parameters.items():
            if isinstance(param_def, dict):
                param_type = param_def.get('type', 'string')
                default_value = param_def.get('defaultValue', param_def.get('default'))
                description = param_def.get('metadata', {}).get('description', '') \
                            if isinstance(param_def.get('metadata'), dict) \
                            else param_def.get('description', '')
                
                self.generator.add_parameter(
                    name=param_name,
                    param_type=param_type,
                    default_value=default_value,
                    description=description
                )
            else:
                # Simple parameter value
                self.generator.add_parameter(
                    name=param_name,
                    param_type='string',
                    default_value=param_def
                )

    def _convert_variables(self, variables: Dict[str, Any]):
        """
        Convert Ev2 variables to Cgv2 format.

        Args:
            variables: Ev2 variables dictionary
        """
        for var_name, var_value in variables.items():
            self.generator.add_variable(var_name, var_value)

    def _convert_rollout_spec(self, rollout_spec: Optional[Dict[str, Any]]):
        """
        Convert Ev2 rollout specification to Cgv2 deployment strategy.

        Args:
            rollout_spec: Ev2 rollout specification
        """
        if not rollout_spec:
            # Default deployment strategy
            self.generator.set_deployment_strategy(strategy="rolling")
            return

        # Extract strategy
        strategy = rollout_spec.get('strategy', 'rolling')
        
        # Extract regions
        regions = rollout_spec.get('regions', rollout_spec.get('targetRegions', []))
        
        # Extract health check configuration
        health_check = rollout_spec.get('healthCheck', rollout_spec.get('safetyCheck'))

        self.generator.set_deployment_strategy(
            strategy=strategy,
            regions=regions,
            health_check=health_check
        )
