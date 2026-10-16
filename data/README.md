# Atlas Data Pipeline

This directory contains the Python data pipeline for downloading and processing OpenStreetMap data.

## Setup on Windows

Open PowerShell in this directory:

```powershell
cd C:\Users\tommy\projects\project-atlas\data
```

Create a virtual environment:

```powershell
python -m venv .venv
```

PowerShell may block the virtual environment activation script. Allow scripts for the current terminal session only:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Activate the environment:

```powershell
.\.venv\Scripts\Activate.ps1
```

The prompt should now begin with `(.venv)`.

Install the pipeline and test dependencies:

```powershell
python -m pip install --upgrade pip
python -m pip install -e ".[pbf, test]"
```

Verify that the virtual environment is being used:

```powershell
python -c "import sys; print(sys.executable)"
```

The printed path should contain `data\.venv\Scripts\python.exe`.

## Run the Pipeline

From the repository root:

```powershell
python -m atlas_data_pipeline --config data\config.json --location malin_Head
```

From this directory:

```powershell
python -m atlas_data_pipeline --config config.json --location malin_Head
```

The command reads the selected location, validates its bounding box, and writes
processed datasets to the configured output directory.

Run tests with:

```powershell
python -m pytest
```

When finished, leave the virtual environment with:

```powershell
deactivate
```

## Without Activating the Environment

If PowerShell activation is unavailable, run commands through the virtual environment directly:

```powershell
.\.venv\Scripts\python.exe -m pip install -e ".[pbf, test]"
.\.venv\Scripts\python.exe -m pytest
```
