# City Generators

Generators convert processed geometry into Unity objects and meshes.

## Folders

- ArchitectureGenerators: building generation and LOD mesh builders
- InfrastructureGenerators: roads and footpaths

## Key scripts

- GroundGenerator.cs: base terrain/ground generation
- ArchitectureGenerators/BuildingGenerator.cs: building object creation
- ArchitectureGenerators/LODGenerators/LOD0MeshBuilder.cs: full-detail building mesh
- ArchitectureGenerators/LODGenerators/LOD1MeshBuilder.cs: medium-detail building mesh
- ArchitectureGenerators/LODGenerators/LOD2MeshBuilder.cs: simplified building mesh
- InfrastructureGenerators/RoadGenerator.cs: road mesh/spline generation
- InfrastructureGenerators/FootpathGenerator.cs: footpath mesh/spline generation

## Notes

- Geometry assumptions (ring cleanup, winding, triangulation) are validated by Editor tests.
- Keep shared math/helpers in City/Utilities rather than duplicating in generators.
