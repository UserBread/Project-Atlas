import os
import logging

from ..osm_utils import (
    build_node_index,
    build_way_index,
    query_overpass,
    relation_lines,
    way_to_coords,
    write_json,
    write_geojson,
)


logger = logging.getLogger(__name__)

QUERY_TEMPLATE = """
[out:json][timeout:{timeout}];
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
    elements = data["elements"]
    node_index = build_node_index(elements)
    way_index = build_way_index(elements)
    features = []

    for element in elements:
        tags = element.get("tags", {})
        if element["type"] == "way":
            coords = way_to_coords(element, node_index)
            if len(coords) < 2:
                continue
            geometry = {"type": "LineString", "coordinates": coords}
        elif element["type"] == "relation":
            lines = relation_lines(element, way_index, node_index)
            if not lines:
                continue
            geometry = {
                "type": "LineString" if len(lines) == 1 else "MultiLineString",
                "coordinates": lines[0] if len(lines) == 1 else lines,
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
                    "height": 0,
                    "layer": "footpath",
                },
            }
        )

    return {"type": "FeatureCollection", "features": features}


def run(bbox, city, output_dir, raw_output_dir, timeout, max_attempts, retry_delay):
    logger.info("Querying footpaths from Overpass API")
    data = query_overpass(
        QUERY_TEMPLATE.format(bbox=bbox, timeout=int(timeout)),
        timeout=timeout,
        max_attempts=max_attempts,
        retry_delay=retry_delay,
    )
    write_json(os.path.join(raw_output_dir, f"{city}_footpaths.json"), data)
    filepath = os.path.join(output_dir, f"{city}_footpaths.geojson")
    write_geojson(filepath, osm_to_footpaths_geojson(data))
    logger.info("Footpaths GeoJSON written path=%s", filepath)
