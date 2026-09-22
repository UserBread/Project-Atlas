# ADR-004: CMake Build System

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Atlas requires reproducible builds across development environments and CI.

## Decision

CMake is the source-of-truth build system.

## Rationale

CMake supports cross-platform configuration, dependency integration, test integration, IDEs and CI.

IDE project files are not authoritative.
