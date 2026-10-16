import os
import logging

from ..osm_utils import build_node_index, query_overpass, way_to_coords, write_json, write_geojson


logger = logging.getLogger(__name__)


QUERY_TEMPLATE = """
[out:json][timeout:{timeout}];
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
        coords = way_to_coords(element, node_index)
        if len(coords) < 2:
            continue
        tags = element.get("tags", {})
        features.append(
            {
                "type": "Feature",
                "id": str(element["id"]),
                "geometry": {"type": "LineString", "coordinates": coords},
                "properties": {
                    "primary_name": tags.get("name"),
                    "height": 0,
                    "layer": "road",
                },
            }
        )
    return {"type": "FeatureCollection", "features": features}


def run(bbox, city, output_dir, raw_output_dir, timeout, max_attempts, retry_delay):
    logger.info("Querying roads from Overpass API")
    data = query_overpass(
        QUERY_TEMPLATE.format(bbox=bbox, timeout=int(timeout)),
        timeout=timeout,
        max_attempts=max_attempts,
        retry_delay=retry_delay,
    )
    write_json(os.path.join(raw_output_dir, f"{city}_roads.json"), data)
    filepath = os.path.join(output_dir, f"{city}_roads.geojson")
    write_geojson(filepath, osm_to_roads_geojson(data))
    logger.info("Roads GeoJSON written path=%s", filepath)
