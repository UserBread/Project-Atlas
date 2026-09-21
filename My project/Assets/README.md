# Assets Folder Guide

This folder contains Unity content used by the city generation project.

## Main folders

- City: active city generation code (data loading, generators, chunking, utilities)
- StreamingAssets: GeoJSON datasets loaded at runtime
- Tests: EditMode tests for geometry, dataset validation, and performance
- Scenes: Unity scenes for running and debugging
- Materials, Shaders, Facade, Settings: art and rendering support assets

## Entry points

- Scene start: Scenes/SampleScene.unity
- Main runtime coordinator: City/CityManager.cs

## Notes

- Tests are documented in Tests/README.md.
