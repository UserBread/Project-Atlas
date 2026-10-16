import os
import logging

from ..osm_utils import (
    build_node_index,
    build_way_index,
    is_closed_ring,
    parse_height_value,
    query_overpass,
    relation_rings,
    way_to_coords,
    write_json,
    write_geojson,
)


logger = logging.getLogger(__name__)

QUERY_TEMPLATE = """
[out:json][timeout:{timeout}];
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
        except (TypeError, ValueError):
            return 0
        building_type = tags.get("building", "yes").lower()
        if building_type in ("commercial", "retail", "office"):
            return levels * 3.3
        if building_type in ("residential", "apartments", "house"):
            return levels * 2.7
        return levels * 3.0

    return 0


def osm_to_buildings_geojson(data):
    elements = data["elements"]
    node_index = build_node_index(elements)
    way_index = build_way_index(elements)
    features = []

    for element in elements:
        tags = element.get("tags", {})
        height = parse_building_height(tags)

        if element["type"] == "way" and "building" in tags:
            coords = way_to_coords(element, node_index)
            if not is_closed_ring(coords):
                continue
            geometry = {"type": "Polygon", "coordinates": [coords]}
        elif element["type"] == "relation" and "building" in tags:
            outers, inners = relation_rings(element, way_index, node_index)
            if not outers:
                continue
            if len(outers) == 1:
                geometry = {"type": "Polygon", "coordinates": [outers[0]] + inners}
            else:
                geometry = {
                    "type": "MultiPolygon",
                    "coordinates": [[outer] for outer in outers],
                }
        else:
            continue

        features.append(
            {
                "type": "Feature",
                "id": str(element["id"]),
                "geometry": geometry,
                "properties": {
                    "primary_name": tags.get("name"),
                    "height": height,
                    "layer": "building",
                },
            }
        )

    return {"type": "FeatureCollection", "features": features}


def run(bbox, city, output_dir, raw_output_dir, timeout, max_attempts, retry_delay):
    logger.info("Querying buildings from Overpass API")
    data = query_overpass(
        QUERY_TEMPLATE.format(bbox=bbox, timeout=int(timeout)),
        timeout=timeout,
        max_attempts=max_attempts,
        retry_delay=retry_delay,
    )
    write_json(os.path.join(raw_output_dir, f"{city}_buildings.json"), data)
    filepath = os.path.join(output_dir, f"{city}_buildings.geojson")
    write_geojson(filepath, osm_to_buildings_geojson(data))
    logger.info("Buildings GeoJSON written path=%s", filepath)
