# ADR-008: Geospatial Data Representation

- **Status:** Accepted
- **Date:** 2026-09-21

## Context

Atlas needs real-world building and road data. Different formats have different strengths.

## Decision

GeoJSON is supported initially because it is easy to inspect and useful for early development.

GeoParquet and DuckDB will be investigated for larger datasets and efficient filtering.

## Rationale

This provides a simple development path while leaving room for scalable data access.

## Consequence

The geospatial layer must be format-independent from procedural generators. Procedural code consumes an internal representation rather than depending directly on a file format.
