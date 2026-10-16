# Data Flow

```text
GeoJSON / GeoParquet
        ↓
Geospatial Loader
        ↓
Internal Geospatial Model
        ↓
Coordinate Conversion
        ↓
Chunk Assignment
        ↓
Procedural Generator
        ↓
CPU Geometry
        ↓
Upload Queue
        ↓
GPU Resources
        ↓
Renderer
```

## Boundary

The procedural layer produces engine-independent CPU-side geometry.

The renderer owns GPU resources.

This prevents procedural algorithms from becoming coupled to OpenGL.
