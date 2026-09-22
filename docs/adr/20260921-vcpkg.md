# ADR-005: vcpkg Dependency Management

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Atlas depends on several third-party C++ libraries.

## Decision

vcpkg manages external C++ dependencies.

Dependencies should be explicitly declared and version-controlled where practical.

## Consequences

Positive:
- Easier setup
- Reproducible dependency configuration
- CI integration

Negative:
- Additional package-manager dependency
- Some packages may require platform-specific handling
