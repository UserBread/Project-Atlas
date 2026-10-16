# ADR-009: OSM Data Ingestion Pipeline

- **Status:** Accepted
- **Date:** 2026-10-16
- **Tags:** geospatial-data python osm overpass pbf geojson

## Context and Problem Statement

Atlas needs repeatable access to OpenStreetMap data for small development scenes and larger benchmark datasets. Overpass is convenient for targeted queries, while large areas require downloading and processing bulk OSM data locally. The project also needs a clear boundary between downloaded source data, generated runtime data, and the C++ application that consumes it.

The former Unity project is historical reference material and is not the target runtime for this pipeline.

## Decision Drivers

- Keep small-area data acquisition simple and quick.
- Support larger regional datasets without depending on a live query service.
- Make imports reproducible and rerunnable.
- Keep the Python importer independent from the runtime application.
- Avoid introducing a server database for a static-dataset workflow.
- Share filtering, normalization, validation, and output logic across data sources.
- Preserve a stable contract for the current C++ pipeline.

## Considered Options

- Overpass only
- Full Planet dump only
- Overpass for small areas and regional OSM PBF extracts for larger areas
- A shared MySQL or PostgreSQL database as the primary exchange format

## Decision Outcome

Chosen option: "Overpass for small areas and regional OSM PBF extracts for larger areas, with shared Python processing and file-based outputs."

The Python pipeline is the official OSM data importer.

Overpass is used for small, targeted bounding boxes and rapid development iteration. Regional PBF extracts are used for larger datasets and repeatable offline processing. A full Planet dump may be supported later for specialised use, but it is not the default bulk source.

The source-specific readers must feed a shared pipeline:

```text
Overpass JSON / OSM PBF
        -> source reader
        -> shared filtering and normalization
        -> geometry validation
        -> runtime artifact writer
        -> C++ application
```

The pipeline is file-based. It does not require MySQL or another server database for the initial implementation. SQLite or GeoPackage may be considered later if repeated spatial queries, incremental updates, or large regional imports justify an intermediate store.

## Artifact Lifecycle

The pipeline uses three artifact categories:

### Source artifacts

Original data downloaded from external sources. These are retained so an import can be reproduced without downloading again.

Examples:

- Overpass JSON responses
- Regional `.osm.pbf` extracts

### Intermediate artifacts

Optional data produced during expensive conversion or normalization. These may be regenerated from source artifacts and are not required by the runtime.

Examples:

- Normalized feature caches
- Temporary extraction databases
- Validation reports

### Runtime artifacts

Final files consumed by the C++ application. They contain only the normalized data required by the application and must follow the versioned output contract.

Initial runtime artifacts are GeoJSON files. They may later be spatially chunked or replaced by a more efficient format without changing the source readers.

Recommended layout:

```text
data/
  raw/
  processed/
  manifests/
  tests/fixtures/
```

Large source downloads and generated datasets are not automatically committed to the repository. The repository must define an explicit policy for dataset storage and distribution.

## Output Contract

The initial runtime contract is:

- GeoJSON `FeatureCollection` root object.
- Coordinates are WGS84 longitude/latitude pairs in GeoJSON order: `[longitude, latitude]`.
- Buildings use `Polygon` or `MultiPolygon` geometry.
- Roads use `LineString` geometry.
- Footpaths use `LineString` or `MultiLineString` geometry.
- OSM identifiers and source element types are preserved in feature properties.
- Dataset-specific metadata such as height and classification is stored under `Feature.properties`.
- The output includes a schema version through its manifest.
- Invalid geometries and unsupported feature types are rejected or reported during validation.

The Python importer does not convert geographic coordinates into rendering coordinates. The C++ application performs local-origin and projected-coordinate conversion according to ADR-007.

## Positive Consequences

- Small datasets remain quick to acquire and inspect.
- Large datasets can be processed offline without stressing Overpass.
- Raw inputs can be retained and conversions can be repeated.
- The C++ application depends on a stable runtime artifact rather than an external service.
- The importer can later add chunked or binary outputs without replacing source acquisition.
- The historical Unity project is no longer an accidental dependency of the active pipeline.

## Negative Consequences

- Two source readers must be maintained.
- PBF processing requires additional tooling and local disk space.
- GeoJSON may become inefficient for large runtime datasets.
- Source data, processed data, and runtime data require storage and cleanup policies.
- OSM relation and multipolygon handling must remain consistent across source formats.

## Operational Requirements

- Record source URL, source timestamp, bounding box or region, query/filter version, converter version, feature counts, and checksums in a manifest.
- Cache raw downloads and verify their integrity before processing.
- Process PBF data as a stream instead of loading an entire regional extract into memory.
- Write runtime files atomically so failed imports cannot publish partial datasets.
- Provide attribution and comply with OpenStreetMap ODbL requirements.
- Use explicit source selection initially. Automatic Overpass/PBF selection should be added only after area, output-size, time, and memory thresholds are measured.

## Links

- Refines [ADR-008: Geospatial Data Representation](20260921-geospatial-data.md)
- Depends on [ADR-007: Local World Coordinate System](20260921-local-coordinate-system.md)
- Supports [ADR-001: Project Scope](20260921-project-scope.md)
