# ADR-006: Chunk-Based World Architecture

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Real-world urban datasets can cover large areas. Keeping every generated object active simultaneously increases memory, CPU and GPU requirements.

## Decision

Atlas represents the world as spatial chunks.

Each chunk has:

- Spatial bounds
- Coordinate
- Generation state
- Generated data
- LOD state
- Render resources or references

## Rationale

Chunking provides a foundation for:

- Streaming
- Culling
- LOD
- Parallel generation
- Spatial queries
- Memory management

## Consequence

Chunk boundaries become an important architectural concept. Roads and intersections crossing boundaries require explicit handling.
