# Atlas Benchmark Plan

## Purpose

Measure whether architectural and optimisation changes improve runtime behaviour.

## Metrics

### Frame

- Average frame time
- 1% low frame time where meaningful
- CPU frame time
- GPU frame time

### Rendering

- Draw calls
- Triangles
- Visible objects
- Visible chunks
- GPU memory

### Generation

- Total generation time
- Per-chunk generation time
- Building generation time
- Road generation time

### Streaming

- Chunk load time
- Chunk unload time
- Queue length
- Streaming stalls

### System

- RAM usage
- Worker utilisation
- Dataset size

## Comparison Rules

Use the same:

- Dataset
- Camera path
- Resolution
- Graphics settings
- Hardware
- Build configuration

Record the software revision.

## Example

```text
Benchmark: Medium City
Build: Release

                 Baseline     Optimised
Frame time       XX ms        YY ms
Draw calls       XXXX         YYYY
Triangles        XXXX         YYYY
Active chunks    XX           YY
Generation       XX s         YY s
Memory           XX MB        YY MB
```

Actual measurements replace placeholders.

## Optimisation Rule

Every significant optimisation should answer:

1. What was slow?
2. Why was it slow?
3. What changed?
4. What does the profiler show now?
5. What was the visual/architectural cost?
