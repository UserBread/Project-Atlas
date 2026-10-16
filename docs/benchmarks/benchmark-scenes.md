# Benchmark Scenes

## Scene A — Small

Purpose:
- Functional correctness
- Debugging
- Rapid iteration

Characteristics:
- Small geographic region
- Low object count
- Short generation time

## Scene B — Medium

Purpose:
- Chunking
- LOD
- Streaming
- Spatial queries

Characteristics:
- Several square kilometres
- Thousands of buildings
- Significant road network

## Scene C — Large

Purpose:
- Stress testing
- Multithreading
- Memory behaviour
- Renderer scalability

Characteristics:
- Large geographic region
- Tens of thousands of buildings or more where available
- Long camera path

## Scene D — Pathological

Purpose:
- Robustness testing

Include:
- Very large footprints
- Dense urban area
- Sparse area
- Complex polygons
- Many adjacent roads
- Large coordinate ranges

Exact datasets should be selected after the data-import pipeline is operational.
