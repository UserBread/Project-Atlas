# Project Atlas Architecture

## Architectural Goal

Atlas separates:

- Application
- World management
- Geospatial data
- Procedural generation
- Rendering
- Streaming
- Profiling

The renderer should not own procedural-generation logic, and procedural generators should not directly depend on OpenGL.

## High-Level Architecture

```text
                         APPLICATION
                              |
        +---------------------+---------------------+
        |                     |                     |
        v                     v                     v
      WORLD                RENDERER             GEOSPATIAL
        |                     |                     |
   +----+----+                |                +----+----+
   |    |    |                |                |         |
 Chunks LOD Streaming         |             Loader  Coordinates
   |                          |                    |
   v                          v                    v
Procedural Generation     OpenGL Backend      Spatial Data
   |
+--+-----------+
|      |       |
v      v       v
Roads Buildings Terrain
```

## Application Layer

Owns:

- Application startup
- System configuration
- Main loop
- Input
- World update
- Rendering coordination
- Shutdown

It should coordinate systems rather than contain their implementation.

## World Layer

Responsibilities:

- Chunk management
- World coordinates
- LOD decisions
- Streaming
- Object ownership
- Spatial queries

Conceptually:

```text
World
├── ChunkManager
├── StreamingManager
├── LODManager
└── SpatialIndex
```

## Geospatial Layer

Responsibilities:

- Load source data
- Validate source data
- Parse geometry
- Extract metadata
- Convert coordinates
- Provide spatial queries

It should output engine-independent structures.

Example:

```text
GeoBuilding
├── footprint
├── attributes
└── height metadata
```

## Coordinate System

Raw latitude/longitude should not be used directly as rendering coordinates.

```text
Geographic coordinates
        ↓
Projection/conversion
        ↓
Local origin
        ↓
Atlas world coordinates
```

The subsystem must define:

- Input CRS
- Projection
- Origin
- Units
- Precision assumptions

## Procedural Generation

```text
Geospatial Object
       ↓
Validation
       ↓
Procedural Parameters
       ↓
Geometry Generation
       ↓
Generated CPU Mesh
```

Generators should not depend directly on OpenGL.

## Building Generator

```text
Footprint
   ↓
Polygon
   ↓
Triangulation
   ↓
Extrusion
   ↓
Facade
   ↓
LOD Meshes
```

Potential output:

```text
BuildingMeshSet
├── LOD0
├── LOD1
└── LOD2
```

## Road Generator

```text
Road Polyline
     ↓
Validation
     ↓
Width / Lane Parameters
     ↓
Offset Geometry
     ↓
Intersection Handling
     ↓
Road Mesh
```

## Renderer

Owns:

- GPU resources
- Shaders
- Camera
- Render queues
- Culling
- Draw submission
- Debug rendering

Rendering flow:

```text
World
  ↓
Visible Chunks
  ↓
LOD Selection
  ↓
Culling
  ↓
Render Queue
  ↓
Batch / Sort
  ↓
OpenGL
```

## GPU Ownership

CPU generation and GPU resources should be separated:

```text
Worker Thread
    ↓
CPU Mesh
    ↓
Upload Queue
    ↓
Render Thread
    ↓
GPU Mesh
```

## Streaming

```text
Camera
   ↓
Streaming Manager
   ├── Load
   ├── Keep
   └── Unload
```

Use independent load/unload thresholds where appropriate to prevent oscillation.

## Multithreading

```text
                    Main Thread
                        |
             +----------+----------+
             |                     |
             v                     v
          Renderer             Job Queue
                                   |
                    +--------------+--------------+
                    |              |              |
                    v              v              v
                 Worker 1       Worker 2       Worker N
                    |              |              |
                    +--------------+--------------+
                                   |
                                   v
                              CPU Results
                                   |
                                   v
                              Upload Queue
                                   |
                                   v
                              Renderer
```

Workers perform CPU-only tasks where possible.

## Spatial Index

Possible implementations:

- Uniform grid
- Quadtree
- BVH
- Spatial hash

The simplest structure that satisfies measured requirements should be preferred.

## LOD

Initial levels:

```text
LOD0 → detailed
LOD1 → simplified
LOD2 → low-poly
```

Selection may depend on camera distance, screen-space importance and performance constraints.

## Memory Ownership

Conceptually:

```text
Dataset
  owns source data

World
  owns active world state

Chunk
  owns generated chunk data

Generator
  produces CPU-side results

Renderer
  owns GPU resources
```

Ownership and lifetime should be explicit.

## Performance Architecture

### CPU

- Data parsing
- Geometry generation
- Spatial queries
- Chunk management

### GPU

- Vertex processing
- Rasterisation
- Shading
- GPU-side operations where later introduced

Profiling should identify whether a bottleneck is CPU-bound, GPU-bound, IO-bound or synchronisation-bound.
