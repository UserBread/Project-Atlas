# Data Pipeline Improvement Checklist

This checklist covers the Python OSM data pipeline, its generated datasets, and the proposed bulk-data path.

## 1. Decide the pipeline boundary

- [X] Confirm that the Python pipeline is still the official data-import pipeline for the project.
- [X] Decide whether the legacy Unity project remains a supported consumer or only a historical reference.
- [X] Define the output contract between the Python pipeline and the current C++ project.
- [X] Keep the importer independent from the Unity-specific `StreamingAssets` path.
- [X] Define which artifacts are source data, intermediate data, and runtime data.

## 2. Make the current pipeline runnable

- [X] Add a dependency definition, preferably `requirements.txt` or `pyproject.toml`.
- [X] Pin or constrain the `requests` version.
- [X] Add a documented virtual-environment setup.
- [ ] Replace hardcoded values in `gatherGeoJSON.py` with CLI arguments or a config file:
  - [ ] City or dataset name.
  - [ ] South, west, north, and east bounds.
  - [ ] Output directory.
  - [ ] Data source.
  - [ ] Overpass timeout and retry settings.
- [ ] Validate the bounding box before making a request.
- [ ] Validate and sanitize names before using them in filenames.
- [ ] Use explicit package imports so the scripts work from the repository root as well as from `legacy/data`.
- [ ] Replace `print` calls with structured logging and useful exit codes.
- [ ] Return a clear error when a dependency, input, response, or output directory is invalid.

## 3. Fix OSM and geometry correctness

- [ ] Preserve OSM IDs and source element types in output properties.
- [ ] Filter returned ways by the tags relevant to the requested dataset. Recursive Overpass output can contain member ways that were not original matches.
- [ ] Prevent duplicate footpath features when both relations and their member ways are returned.
- [ ] Decide whether relation features or member ways are authoritative and apply that rule consistently.
- [ ] Improve multipolygon assembly for buildings:
  - [ ] Support multiple outer rings correctly.
  - [ ] Associate inner rings with the correct outer ring.
  - [ ] Preserve holes for `MultiPolygon` output.
  - [ ] Handle reversed and unordered member ways.
  - [ ] Detect incomplete relations instead of silently dropping them.
- [ ] Validate rings for closure, minimum points, zero area, and invalid coordinates.
- [ ] Detect self-intersecting polygons before exporting them.
- [ ] Make height parsing handle common OSM values such as `10 m`, decimal values, feet, and malformed values explicitly.
- [ ] Define the fallback policy for `building:levels` and document the assumed metres per level.
- [ ] Preserve useful tags such as `building`, `highway`, `surface`, `lanes`, `oneway`, and access restrictions.
- [ ] Confirm that all generated coordinates use the expected GeoJSON order: longitude, latitude.
- [ ] Confirm the coordinate system and local-origin conversion expected by the runtime.

## 4. Define and validate the output schema

- [ ] Write a schema document for the generated GeoJSON.
- [ ] Standardize properties across buildings, roads, and footpaths.
- [ ] Decide whether `height` belongs in `Feature.properties` only; keep it out of the top-level Feature unless explicitly required.
- [ ] Make the Unity/C++ readers match that schema.
- [ ] Include a schema version in every dataset manifest.
- [ ] Validate output before publishing it to the runtime directory.
- [ ] Reject malformed or empty output unless an empty dataset is explicitly allowed.
- [ ] Decide whether pretty-printed JSON is needed outside development builds.

## 5. Improve storage and reproducibility

- [ ] Store raw downloads separately from processed files.
- [ ] Do not commit large raw downloads or generated datasets without an explicit repository policy.
- [ ] Add a manifest for every generated dataset containing:
  - [ ] Source type and URL.
  - [ ] Download timestamp.
  - [ ] OSM data timestamp, when available.
  - [ ] Bounding box or region name.
  - [ ] Query or filter version.
  - [ ] Converter version.
  - [ ] Feature counts.
  - [ ] Checksums.
- [ ] Write generated files atomically through temporary files.
- [ ] Preserve raw Overpass responses so conversion can be repeated without downloading again.
- [ ] Add deterministic ordering of features and properties where practical.
- [ ] Add a cleanup policy for cached downloads and old generated datasets.
- [ ] Keep the first implementation file-based; do not add MySQL for the current static-dataset workflow.
- [ ] Consider SQLite or GeoPackage later if repeated spatial queries, incremental updates, or large regional imports justify it.

Suggested layout:

```text
data/
  raw/
  processed/
  manifests/
  tests/fixtures/
```

## 6. Add the bulk-data path

- [ ] Treat Planet.osm as a bulk PBF download, not as an Overpass-style query API.
- [ ] Prefer regional extracts from Geofabrik or BBBike before attempting the full Planet dump.
- [ ] Add a source interface with separate Overpass and PBF implementations.
- [ ] Keep filtering, normalization, validation, and output writing shared between sources.
- [ ] Choose a PBF reader/tool and document its installation:
  - [ ] `osmium-tool`/`pyosmium`.
  - [ ] `pyrosm`.
  - [ ] Another justified option.
- [ ] Stream PBF processing instead of loading the entire dataset into memory.
- [ ] Cache downloaded PBF files and verify their checksums.
- [ ] Support resumable downloads where practical.
- [ ] Define how the source selector chooses Overpass versus PBF.
- [ ] Use an explicit user-selected source initially; add automatic selection only after measuring area and output size.
- [ ] Record the source and source version in the manifest.
- [ ] Ensure relation processing works consistently for Overpass JSON and PBF input.
- [ ] Add a small PBF fixture for automated tests.

## 7. Plan for large outputs

- [ ] Measure generated GeoJSON size, conversion time, memory use, and Unity load time.
- [ ] Avoid loading full regional datasets into one in-memory JSON object.
- [ ] Add spatial chunking when files become too large.
- [ ] Define a chunk naming and indexing convention.
- [ ] Consider FlatGeobuf, GeoPackage, or another binary format if GeoJSON becomes a bottleneck.
- [ ] Keep a simple GeoJSON export for debugging and small datasets.
- [ ] Ensure chunk boundaries do not incorrectly cut buildings, roads, or relation geometries.
- [ ] Add benchmark datasets for small, medium, large, and pathological cases.

## 8. Add automated tests

- [ ] Test bounding-box validation.
- [ ] Test query construction and escaping.
- [ ] Test Overpass retry and failure behaviour without making real network requests.
- [ ] Test malformed and empty API responses.
- [ ] Test missing nodes and incomplete ways.
- [ ] Test height parsing and level-based height fallback.
- [ ] Test closed and open ways.
- [ ] Test single-outer multipolygons with holes.
- [ ] Test multi-outer multipolygons with holes.
- [ ] Test reversed and unordered relation members.
- [ ] Test footpath relation/member deduplication.
- [ ] Test output schema validation.
- [ ] Test deterministic output and manifest generation.
- [ ] Test Overpass JSON and PBF readers against the same expected features.
- [ ] Add an end-to-end test using local fixtures rather than live Overpass data.
- [ ] Keep expensive large-data tests explicit and separately runnable.

## 9. Improve documentation

- [ ] Replace manual editing instructions with the actual CLI/configuration workflow.
- [ ] Document installation and supported Python versions.
- [ ] Document Overpass limits, retry behaviour, and expected runtime.
- [ ] Document when to use Overpass, a regional PBF, or the full Planet dump.
- [ ] Document the output schema and coordinate system.
- [ ] Document raw, processed, cached, and runtime output locations.
- [ ] Document attribution and ODbL obligations for OpenStreetMap data.
- [ ] Remove or clearly label the manual Overpass Turbo workflow as a fallback.
- [ ] Fix documentation typos such as `retireving`.
- [ ] Add troubleshooting for missing `requests`, failed Overpass endpoints, malformed data, and insufficient disk space.

## Suggested implementation order

1. Define the active pipeline and output schema.
2. Make the current Overpass pipeline reproducible and configurable.
3. Add schema validation and Python fixture tests.
4. Fix relation handling and duplicate feature generation.
5. Add manifests, caching, and atomic output writes.
6. Add a regional PBF reader behind the same pipeline interface.
7. Benchmark output size and memory use.
8. Add chunked or binary output only when measurements justify it.
9. Revisit SQLite/GeoPackage only if repeated spatial queries or incremental imports become necessary.
