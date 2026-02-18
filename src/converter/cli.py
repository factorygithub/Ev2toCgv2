"""
Command Line Interface for Ev2 to Cgv2 Converter
"""

import argparse
import sys
from pathlib import Path
from .converter import Ev2ToCgv2Converter


def main():
    """Main CLI entry point."""
    parser = argparse.ArgumentParser(
        description="Convert Azure Express V2 (Ev2) deployment specifications to Cloud Generation V2 (Cgv2) format"
    )
    
    parser.add_argument(
        'input',
        help='Path to input Ev2 file (JSON or YAML)'
    )
    
    parser.add_argument(
        'output',
        help='Path to output Cgv2 file'
    )
    
    parser.add_argument(
        '-f', '--format',
        choices=['json', 'yaml'],
        default='json',
        help='Output format (default: json)'
    )
    
    parser.add_argument(
        '-v', '--verbose',
        action='store_true',
        help='Enable verbose output'
    )

    args = parser.parse_args()

    try:
        # Verify input file exists
        if not Path(args.input).exists():
            print(f"Error: Input file not found: {args.input}", file=sys.stderr)
            return 1

        if args.verbose:
            print(f"Converting {args.input} to {args.output}...")

        # Perform conversion
        converter = Ev2ToCgv2Converter()
        result = converter.convert_file(args.input, args.output, args.format)

        if args.verbose:
            print(f"Successfully converted to {args.format.upper()} format")
            print(f"Output saved to: {args.output}")
            print(f"Resources converted: {len(result.get('spec', {}).get('resources', []))}")

        return 0

    except FileNotFoundError as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1
    except ValueError as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1
    except Exception as e:
        print(f"Unexpected error: {e}", file=sys.stderr)
        if args.verbose:
            import traceback
            traceback.print_exc()
        return 1


if __name__ == '__main__':
    sys.exit(main())
