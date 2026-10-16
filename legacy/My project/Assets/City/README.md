# City Module

This folder contains the active city-generation implementation.

## Structure

- Data: GeoJSON loading and coordinate conversion helpers
- Generators: building, road, footpath, and ground generation
- Chunk: runtime chunk and LOD lifecycle management
- Utilities: shared math, rendering presets, and performance collection
- CityManager.cs: top-level coordinator

## Typical flow

1. Data/DatasetLoader loads GeoJSON features from StreamingAssets.
2. Data/GeometryConverter prepares geometry and origin/bounds values.
3. Generators create meshes/objects for architecture and infrastructure.
4. Chunk systems stream and manage city chunks by camera/player position.
5. Utilities provide shared calculations and runtime metrics.
