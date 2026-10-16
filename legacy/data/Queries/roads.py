import os
from Utilities.osm_utils import (
    query_overpass,
    build_node_index,
    way_to_coords,
    write_geojson,
)

QUERY_TEMPLATE = """
[out:json][timeout:180];
(
  way["highway"~"motorway|trunk|primary|secondary|tertiary|unclassified|residential|service|living_street|road"]({bbox});
);
out body;
>;
out skel qt;
"""


def osm_to_roads_geojson(data):
    node_index = build_node_index(data["elements"])
    features = []

    for element in data["elements"]:
        if element["type"] != "way":
            continue

        tags = element.get("tags", {})
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
                "layer": "road"
            }
        })

    return {"type": "FeatureCollection", "features": features}


def run(bbox, city, output_dir):
    print("Querying roads from Overpass API...")
    data = query_overpass(QUERY_TEMPLATE.format(bbox=bbox))
    geojson = osm_to_roads_geojson(data)
    filepath = os.path.join(output_dir, f"{city}_roads.geojson")
    write_geojson(filepath, geojson)
    print(f"Roads GeoJSON written to: {filepath}")
