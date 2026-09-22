# Unity Test Suite

This folder contains EditMode tests for core city-generation logic, data validation, and performance/load checks.

## Test Files

### AlgorithmTests
File: `Editor/AlgorithmTests.cs`

Covers:
- Polygon winding sign check (`MeshUtils.SignedArea`)
- Convex and concave triangulation correctness (`MeshUtils.Triangulate`)
- Ring cleanup behavior (`BuildingMeshHelper.CleanupRingPoints`)
- LOD0 mesh generation success/failure paths (`LOD0MeshBuilder.GenerateLOD0Mesh`)
- Road and footpath spline behavior via reflection (`CatmullRomSpline`)

### GeoUtilsTests
File: `Editor/GeoUtilsTests.cs`

Covers:
- Mercator conversion at origin (`GeoUtils.latLonToMercator`)
- Unity position conversion relative to origin (`GeoUtils.latLonToUnityPosition`)
- Direction/sign checks for east+north offsets
- Consistency between Mercator delta and Unity offsets

### DataValidationTests
File: `Editor/DataValidationTests.cs`

Covers:
- Required StreamingAssets datasets exist
- GeoJSON root format is `FeatureCollection`
- Building geometries are polygonal (`Polygon`/`MultiPolygon`)
- Road/footpath geometries are linear (`LineString`/`MultiLineString`)
- Coordinate arrays have valid shape and numeric lon/lat
- Geometry origin/center extraction works for real datasets

### PerformanceAndLoadTests
File: `Editor/PerformanceAndLoadTests.cs`

Covers:
- Projection throughput budget (`GeoUtils`)
- Triangulation throughput budget (`MeshUtils`)
- Repeated LOD0 mesh generation budget
- Explicit load tests for large dataset parsing and bounding-box calculation

## How to Run

### Unity Editor
1. Open the project.
2. Go to **Window -> General -> Test Runner**.
3. Select **EditMode**.
4. Click **Run All**.

## Notes

- Tests under `PerformanceAndLoadTests` marked with `[Explicit]` do not run in a normal "Run All" unless explicitly selected.
- Data validation tests rely on current files in `Assets/StreamingAssets`.

## Future TODO

- Add a PlayMode smoke suite for scene startup and generator completion.
- Add end-to-end failure-path tests (e.g., malformed/missing GeoJSON and empty datasets).
