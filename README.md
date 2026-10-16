# Project Atlas

> A C++ real-time procedural urban environment system for turning real-world geospatial data into large, navigable, efficiently rendered cities.

## Overview

Project Atlas is a systems-focused C++ project for procedural generation, geospatial processing, real-time rendering, world streaming, and performance engineering.

Atlas takes real-world geospatial data and transforms it into a coherent 3D urban environment.

```text
Geospatial Data
      ↓
Data Loading
      ↓
Coordinate Conversion
      ↓
Procedural Generation
   ↙          ↘
Buildings     Roads
   ↘          ↙
    World Chunks
         ↓
        LOD
         ↓
     Streaming
         ↓
  Real-Time Rendering
```

Atlas is deliberately not initially a game. It is an urban-environment technology project that can later become a foundation for a game, simulation, visualisation tool, or other application.

## Core Objective

Build a C++ real-time procedural urban environment system capable of:

1. Importing real-world geospatial data.
2. Converting geographic coordinates into a local world coordinate system.
3. Generating procedural buildings and roads.
4. Organising the world into spatial chunks.
5. Selecting appropriate levels of detail.
6. Streaming world regions based on camera/player position.
7. Generating data asynchronously where appropriate.
8. Rendering large urban environments efficiently.
9. Measuring and optimising CPU/GPU performance.

## Initial MVP

```text
GeoJSON / GeoParquet
        ↓
Coordinate Conversion
        ↓
Building Footprints
        ↓
Building Mesh Generation
        ↓
OpenGL Renderer
```

Larger MVP:

```text
Real-World Data
      ↓
Coordinates
      ↓
Buildings + Roads
      ↓
Chunks
      ↓
LOD
      ↓
Streaming
      ↓
Real-Time World
```

## Scope

### Initially supported

- C++20 application
- OpenGL rendering
- Real-world geospatial data
- Coordinate conversion
- Procedural buildings
- Procedural roads
- Chunk-based world
- Level of detail
- Frustum/distance culling
- Streaming
- Multithreaded generation
- Developer tools
- Profiling
- Benchmarking

### Initially out of scope

- NPCs
- Gameplay
- Traffic simulation
- Multiplayer
- Destruction
- Advanced physics
- Full building interiors
- Complete weather simulation
- Photorealistic rendering
- VR

## Design Principles

### Systems before game
Atlas should remain useful without a game attached.

### Measure before optimising
Performance decisions should be supported by profiling and benchmarks.

### Explicit ownership
CPU data, world data and GPU resources should have clear owners.

### Renderer independence
Procedural generation should not directly depend on OpenGL.

### Controlled complexity
Multithreading, spatial indexes and advanced rendering should be introduced when they solve measurable problems.

## Technology

- C++20
- CMake
- vcpkg
- OpenGL 4.6
- GLFW
- GLAD
- GLM
- Dear ImGui
- spdlog
- nlohmann/json
- Catch2
- Tracy
- DuckDB
- GitHub Actions
- clang-format
- clang-tidy
- AddressSanitizer
- UndefinedBehaviorSanitizer

## Repository Structure

```text
Project-Atlas/
├── README.md
├── ROADMAP.md
├── ARCHITECTURE.md
├── TECH_STACK.md
├── RESEARCH.md
├── LICENSE
├── CMakeLists.txt
├── vcpkg.json
├── docs/
│   ├── decisions/
│   ├── architecture/
│   └── benchmarks/
├── src/
├── include/
├── tests/
├── assets/
└── data/
```

## Status

**Current phase:** Foundation / Week 0.

## Planned Releases

| Version | Focus |
|---|---|
| 0.1 | Engine foundation |
| 0.2 | Renderer |
| 0.3 | Geospatial pipeline |
| 0.4 | Buildings |
| 0.5 | Roads |
| 0.6 | World chunks |
| 0.7 | Streaming |
| 0.8 | Multithreading |
| 0.9 | Optimisation and tooling |
| 1.0 | Real-time procedural urban environment |

## Licence

Licence to be selected before the first public release.
