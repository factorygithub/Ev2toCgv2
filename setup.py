from setuptools import setup, find_packages

setup(
    name="ev2-to-cgv2-converter",
    version="0.1.0",
    description="A tool to convert Azure Express V2 (Ev2) deployment specifications to Cloud Generation V2 (Cgv2) format",
    author="Factory GitHub",
    packages=find_packages(where="src"),
    package_dir={"": "src"},
    python_requires=">=3.7",
    install_requires=[
        "pyyaml>=6.0.1",
        "jsonschema>=4.17.0",
    ],
    entry_points={
        "console_scripts": [
            "ev2-to-cgv2=converter.cli:main",
        ],
    },
)
