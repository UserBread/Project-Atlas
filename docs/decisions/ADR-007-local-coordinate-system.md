# ADR-007: Local World Coordinate System

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Latitude/longitude coordinates are unsuitable as direct rendering coordinates. Large geographic coordinates can also introduce floating-point precision problems.

## Decision

Atlas converts source geographic coordinates into a projected/local coordinate system before procedural generation and rendering.

The system maintains an explicit world origin and documents coordinate assumptions.

## Requirements

The coordinate subsystem must define:

- Input CRS
- Projection
- Origin
- Units
- Precision
- Conversion direction
- Height datum assumptions where relevant

## Consequence

All spatial systems must use a consistent coordinate convention. The coordinate subsystem is foundational and must be thoroughly tested.
