# ADR-001: Project Scope

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

The project could expand into a complete game engine, city simulator or game. That would obscure the primary goal: investigating procedural urban-environment systems and runtime performance.

## Decision

Atlas initially focuses on:

- Geospatial data ingestion
- Coordinate conversion
- Procedural urban geometry
- World chunking
- LOD
- Streaming
- Multithreaded generation
- Real-time rendering
- Profiling and benchmarking

Initially out of scope:

- Gameplay
- NPCs
- Traffic simulation
- Multiplayer
- Destruction
- Advanced physics
- Full interiors
- VR
- Complete weather systems

## Rationale

A systems-first scope keeps the project technically focused and allows each subsystem to be benchmarked independently.

## Consequence

The initial application may look like a technical visualisation rather than a game. This is intentional.
