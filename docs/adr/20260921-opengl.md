# ADR-003: OpenGL as Initial Graphics API

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Atlas needs a graphics API that permits direct investigation of renderer architecture without relying on a full game engine.

## Decision

OpenGL 4.6 is the initial graphics API.

## Rationale

OpenGL provides a mature ecosystem, broad support and direct access to common rendering concepts while keeping initial complexity manageable.

## Alternatives

### Vulkan

More explicit control but substantially more complexity.

### Direct3D 12

Strong Windows ecosystem but not necessary for the initial objective.

### Unreal Engine

Would provide rendering infrastructure but would remove much of the renderer/system implementation Atlas is intended to investigate.

## Consequence

Atlas gains direct renderer control but does not initially inherit the facilities of a modern game engine.

A different graphics backend can be investigated later if justified.
