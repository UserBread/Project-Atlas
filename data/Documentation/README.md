# How to run the GeoJSON Gatherer

Fetches building, road, and footpath data from the [Overpass API](https://overpass-api.de/) for a given bounding box and writes GeoJSON files to the Unity project's `StreamingAssets` folder.

## Other method of retireving data
[Web-based Retrieval](./webApp.md) 

## Prerequisites

- Python 3.8+
- `requests` library

```bash
pip install requests
```

## Configuration

Open `gatherGeoJSON.py` and edit the variables at the top of the file:

| Variable | Description | Example |
|---|---|---|
| `bby_min` | South boundary (latitude) | `55.34` |
| `bbx_min` | West boundary (longitude) | `-7.35` |
| `bby_max` | North boundary (latitude) | `55.37` |
| `bbx_max` | East boundary (longitude) | `-7.29` |
| `city` | Name prefix for output files | `"malinHead"` |

## Running

From the `data/` directory:

```bash
python gatherGeoJSON.py
```

## Output

Three GeoJSON files are written to `My project/Assets/StreamingAssets/`:

- `<city>_buildings.geojson`
- `<city>_roads.geojson`
- `<city>_footpaths.geojson`

## File Structure

| File | Purpose |
|---|---|
| `gatherGeoJSON.py` | Entry point — set config here and run |
| `Queries/buildings.py` | Buildings query and GeoJSON conversion |
| `Queries/roads.py` | Roads query and GeoJSON conversion |
| `Queries/footpaths.py` | Footpaths query and GeoJSON conversion |
| `osm_utils.py` | Shared helpers |
