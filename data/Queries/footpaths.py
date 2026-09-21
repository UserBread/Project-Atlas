import os
from Utilities.osm_utils import (
    query_overpass,
    build_node_index,
    build_way_index,
    way_to_coords,
    relation_lines,
    write_geojson,
)

QUERY_TEMPLATE = """
[out:json][timeout:180];
(
  way["highway"~"footway|pedestrian|steps"]({bbox});
  way["highway"="path"]["foot"~"yes|designated"]({bbox});
  relation["highway"~"footway|pedestrian|steps"]({bbox});
);
out body;
>;
out skel qt;
"""


def osm_to_footpaths_geojson(data):
    node_index = build_node_index(data["elements"])
    way_index = build_way_index(data["elements"])
    features = []

    for element in data["elements"]:
        tags = element.get("tags", {})

        if element["type"] == "way":
            coords = way_to_coords(element, node_index)
            if len(coords) < 2:
                continue

            features.append({
                "type": "Feature",
                "id": str(element["id"]),
                "geometry": {
                    "type": "LineString",
                    "coordinates": coords
                },
                "properties": {
                    "primary_name": tags.get("name"),
                    "height": 0,
                    "layer": "footpath"
                }
            })

        elif element["type"] == "relation":
            lines = relation_lines(element, way_index, node_index)
            if not lines:
                continue

            if len(lines) == 1:
                geometry = {
                    "type": "LineString",
                    "coordinates": lines[0]
                }
            else:
                geometry = {
                    "type": "MultiLineString",
                    "coordinates": lines
                }

            features.append({
                "type": "Feature",
                "id": str(element["id"]),
                "geometry": geometry,
                "properties": {
                    "primary_name": tags.get("name"),
                    "height": 0,
                    "layer": "footpath"
                }
            })

    return {"type": "FeatureCollection", "features": features}


def run(bbox, city, output_dir):
    print("Querying footpaths from Overpass API...")
    data = query_overpass(QUERY_TEMPLATE.format(bbox=bbox))
    geojson = osm_to_footpaths_geojson(data)
    filepath = os.path.join(output_dir, f"{city}_footpaths.geojson")
    write_geojson(filepath, geojson)
    print(f"Footpaths GeoJSON written to: {filepath}")
