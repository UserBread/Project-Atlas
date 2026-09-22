import os
from Utilities.osm_utils import (
    query_overpass,
    build_node_index,
    build_way_index,
    way_to_coords,
    is_closed_ring,
    parse_height_value,
    relation_rings,
    write_geojson,
)

QUERY_TEMPLATE = """
[out:json][timeout:180];
(
  way["building"]({bbox});
  relation["building"]["type"="multipolygon"]({bbox});
);
out body;
>;
out skel qt;
"""


def parse_building_height(tags):
    if "height" in tags:
        height = parse_height_value(tags["height"])
        if height > 0:
            return height

    if "building:levels" in tags:
        try:
            levels = float(tags["building:levels"])
            building_type = tags.get("building", "yes").lower()

            if building_type in ("commercial", "retail", "office"):
                return levels * 3.3
            if building_type in ("residential", "apartments", "house"):
                return levels * 2.7

            return levels * 3.0
        except ValueError:
            return 0

    return 0


def osm_to_buildings_geojson(data):
    node_index = build_node_index(data["elements"])
    way_index = build_way_index(data["elements"])
    features = []

    for element in data["elements"]:
        tags = element.get("tags", {})
        height = parse_building_height(tags)

        if element["type"] == "way" and "building" in tags:
            coords = way_to_coords(element, node_index)
            if not is_closed_ring(coords):
                continue

            features.append({
                "type": "Feature",
                "id": str(element["id"]),
                "geometry": {
                    "type": "Polygon",
                    "coordinates": [coords]
                },
                "properties": {
                    "primary_name": tags.get("name"),
                    "height": height,
                    "layer": "building"
                }
            })

        elif element["type"] == "relation" and "building" in tags:
            outers, inners = relation_rings(element, way_index, node_index)

            if not outers:
                continue

            if len(outers) == 1:
                geometry = {
                    "type": "Polygon",
                    "coordinates": [outers[0]] + inners
                }
            else:
                geometry = {
                    "type": "MultiPolygon",
                    "coordinates": [[outer] for outer in outers]
                }

            features.append({
                "type": "Feature",
                "id": str(element["id"]),
                "geometry": geometry,
                "properties": {
                    "primary_name": tags.get("name"),
                    "height": height,
                    "layer": "building"
                }
            })

    return {"type": "FeatureCollection", "features": features}


def run(bbox, city, output_dir):
    print("Querying buildings from Overpass API...")
    data = query_overpass(QUERY_TEMPLATE.format(bbox=bbox))
    geojson = osm_to_buildings_geojson(data)
    filepath = os.path.join(output_dir, f"{city}_buildings.geojson")
    write_geojson(filepath, geojson)
    print(f"Buildings GeoJSON written to: {filepath}")
