# Project Atlas Technology Stack

## Language

### C++20

Chosen for:

- Native performance
- Explicit resource management
- Memory control
- Concurrency
- Graphics programming
- Engine/system development

## Build

### CMake

Source-of-truth build system for configuration, compilation, testing and CI.

## Dependencies

### vcpkg

Dependency manager for reproducible C++ dependencies.

## Graphics

### OpenGL 4.6

Initial graphics API.

Chosen because it provides direct access to renderer concepts without requiring a full engine.

## Windowing

### GLFW

Window creation, OpenGL context and input abstraction.

## OpenGL Loading

### GLAD

OpenGL function loading.

## Mathematics

### GLM

Vectors, matrices, transforms, camera and projection calculations.

## Developer UI

### Dear ImGui

Runtime debugging and profiling interface.

## Logging

### spdlog

Structured application logging.

## JSON

### nlohmann/json

Configuration and lightweight metadata where JSON is appropriate.

## Testing

### Catch2

Unit and system tests.

Important test areas:

- Geometry
- Coordinates
- Chunks
- Data validation
- Procedural generation

## Profiling

### Tracy

CPU/GPU profiling, thread activity and performance investigations.

## Geospatial

### GeoJSON

Initial interchange format for development and debugging.

### GeoParquet / DuckDB

Investigated for larger datasets and efficient filtering/querying.

## Code Quality

- clang-format
- clang-tidy

## Sanitizers

- AddressSanitizer
- UndefinedBehaviorSanitizer

## CI

GitHub Actions:

```text
Commit
  ↓
Configure
  ↓
Build
  ↓
Tests
  ↓
Static Checks
  ↓
Sanitizer Tests
```

## Architecture Principle

Procedural generation should not depend directly on OpenGL.

The dependency direction should remain roughly:

```text
Application
    ↓
World
    ├── Procedural
    └── Geospatial

Renderer
    ↓
Graphics Backend
```

## Technology Selection Principle

Avoid adding technology for its own sake.

Do not introduce premature:

- ECS architecture
- Custom allocators
- Complex job systems
- Multiple graphics APIs
- Large engine frameworks
- Advanced GPU systems

unless they solve a demonstrated requirement.
