# Project Atlas Research

## Research Purpose

Atlas investigates how real-world geospatial information can be transformed into a large procedural urban environment while maintaining acceptable runtime performance.

The project is primarily an engineering and systems investigation rather than a claim of a novel procedural-generation algorithm.

## Central Technical Question

> How can real-world geospatial datasets be transformed into a coherent procedural urban environment in C++ while maintaining scalable generation, streaming and real-time rendering performance?

## Supporting Questions

### Geospatial

- How should geographic coordinates be represented?
- How large can the local coordinate system become before precision becomes problematic?
- How should source geometry be validated?

### Procedural generation

- How efficiently can building footprints be triangulated and extruded?
- How should building variation be represented?
- How should road geometry be generated?
- How should intersections be handled?

### World representation

- What chunk size balances management overhead and streaming granularity?
- Should spatial organisation use a grid, quadtree, BVH or another structure?
- Should chunks own geometry or reference shared resources?

### Streaming

- How much data should remain active?
- What loading/unloading strategy minimises stalls?
- How should streaming priorities be calculated?

### Multithreading

- Which operations are safe to perform asynchronously?
- How should generation results reach the render thread?
- Where do synchronisation costs become significant?

### Rendering

- When is rendering CPU-bound?
- When is it GPU-bound?
- How much do LOD, culling and batching improve frame time?

## Performance Hypotheses

### H1 — Spatial partitioning reduces unnecessary work

Partitioning the world should reduce the amount of geometry and metadata processed for a given camera view.

### H2 — LOD reduces rendering cost

Lower-detail representations at distance should reduce GPU workload while maintaining acceptable visual quality.

### H3 — Background generation improves frame stability

Moving expensive CPU generation away from the render thread should reduce frame-time spikes.

### H4 — Culling reduces rendering workload

Frustum and distance culling should reduce unnecessary draw submissions and/or geometry processing.

### H5 — Batching reduces CPU rendering overhead

Reducing draw calls should improve CPU-side rendering performance when submission is a bottleneck.

These are hypotheses to test, not assumptions.

## Benchmark Variables

Record:

- World area
- Building count
- Road count
- Triangle count
- Active chunks
- Loaded chunks
- Draw calls
- CPU frame time
- GPU frame time
- Generation time
- Streaming time
- Memory
- Worker utilisation

## Benchmark Methodology

Compare baseline and optimised versions under the same:

- Dataset
- Camera path
- Resolution
- Graphics settings
- Hardware
- Build configuration

Record the software revision.

## Failure Modes

### Geometry

- Invalid polygons
- Self-intersections
- Degenerate triangles
- Complex footprints

### Coordinates

- Precision loss
- Projection errors
- Large-world jitter
- Origin errors

### Streaming

- Stalls
- Pop-in
- Race conditions
- Duplicate generation
- Failed unloads

### Concurrency

- Data races
- Deadlocks
- Lifetime errors
- Queue contention

### Rendering

- Excessive draw calls
- GPU memory pressure
- CPU bottlenecks
- Poor batching
- LOD popping

## Relationship to Previous Work

Atlas is a systems-oriented evolution of the earlier real-world procedural urban environment implementation.

The previous implementation should be treated as a reference implementation and source of test cases, not as an architecture Atlas must preserve.

Atlas provides an opportunity to:

- Reconsider architecture
- Separate engine-independent systems
- Measure bottlenecks
- Introduce explicit ownership
- Introduce controlled multithreading
- Build a renderer directly
- Compare implementation strategies

## Evaluation Principle

Prefer measurable claims:

> "Chunk streaming reduced CPU frame time from X ms to Y ms in benchmark B."

rather than:

> "Chunk streaming made the city faster."

## Long-Term Research

Potential investigations:

- GPU-driven rendering
- Compute-based culling
- Hierarchical LOD
- Procedural terrain
- Vegetation
- Advanced road networks
- Building interiors
- Persistent world data
- Large-world coordinate strategies
- Alternative graphics APIs
