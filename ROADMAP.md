# Project Atlas Roadmap

## Development Strategy

Atlas is developed from the bottom up:

```text
C++ Foundations
      ↓
Application
      ↓
Renderer
      ↓
Geospatial Data
      ↓
Procedural Geometry
      ↓
World Representation
      ↓
Streaming
      ↓
Parallel Generation
      ↓
Optimisation
      ↓
Advanced Systems
```

Each phase should produce a working system before the next layer is introduced.

## Phase 0 — Definition and Foundation

- Define scope and MVP
- Create repository
- Establish architecture
- Select technologies
- Create ADRs
- Configure CMake/vcpkg
- Configure CI
- Define benchmark strategy

**Done when:** a fresh clone can build the project and the documentation explains the architecture.

## Phase 1 — C++ Engine Skeleton

- Application class
- Main loop
- Window management
- Input abstraction
- Time management
- Logging
- Configuration
- Debug/release builds
- Basic testing

**Target:**

```text
Application
    ↓
Main Loop
    ├── Update
    ├── Render
    └── Input
```

## Phase 2 — Renderer

- OpenGL context
- Shader abstraction
- Vertex/index buffers
- Vertex arrays
- Textures
- Camera
- Projection
- Basic lighting
- Mesh abstraction
- ImGui

**Done when:** Atlas renders a 3D test scene with developer UI.

## Phase 3 — Renderer Architecture

- Renderable abstraction
- GPU resource ownership
- Materials
- Render queues
- Camera/frustum
- Debug rendering
- Resource lifetime rules

## Phase 4 — Geospatial Data

- GeoJSON
- GeoParquet/DuckDB investigation
- Building extraction
- Road extraction
- Data validation
- Spatial bounds
- Coordinate metadata

**Done when:** a real-world dataset can be loaded into an internal representation.

## Phase 5 — Coordinate System

- Geographic coordinate representation
- Projection/conversion
- Local origin
- Units
- Precision analysis
- Conversion tests

The coordinate system must be documented before large procedural systems depend on it.

## Phase 6 — Procedural Buildings

```text
Footprint
   ↓
Polygon Validation
   ↓
Triangulation
   ↓
Extrusion
   ↓
Facade
   ↓
LOD Meshes
```

Tasks:

- Polygon representation
- Validation
- Triangulation
- Extrusion
- Floors
- Roofs
- Doors
- Windows
- Metadata
- LODs

## Phase 7 — Roads

- Polyline processing
- Road width
- Offset geometry
- Lane representation
- Intersections
- Junction handling
- Mesh generation
- Road LOD
- Metadata

## Phase 8 — World Chunks

- Chunk coordinate system
- Chunk bounds
- World-to-chunk conversion
- Chunk data
- Chunk manager
- Chunk lookup
- Debug rendering

Lifecycle:

```text
UNLOADED → LOADING → GENERATING → UPLOADING → ACTIVE → UNLOADING → UNLOADED
```

## Phase 9 — Streaming

- Camera/player position
- Load radius
- Unload radius
- Priority ordering
- Background loading
- Activation/deactivation
- Streaming metrics

## Phase 10 — Multithreaded Generation

- Worker threads
- Job queue
- Generation jobs
- Thread-safe queues
- Cancellation
- Synchronisation
- Main-thread GPU upload

**Important:** OpenGL resource ownership must remain compatible with the context/thread model.

## Phase 11 — Spatial Optimisation

Investigate and benchmark:

- Uniform grid
- Quadtree
- BVH
- Spatial hashing
- Frustum culling
- Distance culling

## Phase 12 — LOD

Initial levels:

```text
LOD0 — detailed
LOD1 — simplified
LOD2 — low-poly
```

Tasks:

- LOD selection
- Distance thresholds
- Hysteresis
- Chunk-level LOD
- Object-level LOD
- LOD metrics

## Phase 13 — Rendering Optimisation

Investigate:

- Batching
- Instancing
- Draw-call reduction
- Frustum culling
- GPU buffer management
- Mesh reuse
- Material batching

All significant optimisation decisions should be benchmark-driven.

## Phase 14 — Profiling

Track:

- Frame time
- CPU time
- GPU time
- Generation time
- Streaming time
- Draw calls
- Triangles
- Active chunks
- Memory
- Worker utilisation

## Phase 15 — Developer Tools

Dear ImGui panels for:

- FPS/frame time
- Chunk map
- Chunk states
- LOD view
- Wireframe
- Bounding boxes
- Spatial grid
- Generation queue
- Streaming queue
- Memory
- Renderer statistics

## Phase 16 — Advanced Procedural Generation

Potential additions:

- Building archetypes
- Facade styles
- Roof variation
- Street furniture
- Vegetation
- Terrain
- Bridges
- Parks
- Interiors
- Procedural street details

## Phase 17 — Advanced Rendering

Potential additions:

- Shadow mapping
- HDR
- Ambient occlusion
- Sky
- Atmospheric effects
- Water
- Terrain rendering
- Improved materials

## Phase 18 — Stress Testing

Progression:

```text
Small town
   ↓
Large town
   ↓
City district
   ↓
Large city
```

Measure generation, streaming, frame time, memory, triangles and draw calls.

## Phase 19 — Benchmarking

Create repeatable benchmark scenes at increasing scales.

## Phase 20 — Final Architecture

Document:

- Component boundaries
- Thread ownership
- Memory ownership
- GPU ownership
- Data flow
- Performance characteristics
- Known limitations
- Extension points

The final architecture should reflect what was learned during implementation.
