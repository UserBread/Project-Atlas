import json
import time
import requests

OVERPASS_URLS = [
    "https://overpass-api.de/api/interpreter",
    "https://lz4.overpass-api.de/api/interpreter",
    "https://z.overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter"
]


def query_overpass(query):
    headers = {
        "User-Agent": "fyp-tommy-bui/1.0 (+https://github.com/)"
    }

    max_attempts_per_endpoint = 2
    last_error = None

    for endpoint in OVERPASS_URLS:
        for attempt in range(1, max_attempts_per_endpoint + 1):
            try:
                response = requests.post(
                    endpoint,
                    data={"data": query},
                    headers=headers,
                    timeout=180
                )
                response.raise_for_status()
                return response.json()
            except requests.exceptions.RequestException as ex:
                last_error = ex
                print(f"Overpass request failed ({endpoint}, attempt {attempt}/{max_attempts_per_endpoint}): {ex}")
                time.sleep(2 * attempt)

    raise RuntimeError(f"All Overpass endpoints failed. Last error: {last_error}")


def build_node_index(elements):
    return {e["id"]: e for e in elements if e["type"] == "node"}


def build_way_index(elements):
    return {e["id"]: e for e in elements if e["type"] == "way"}


def way_to_coords(way, node_index):
    coords = []
    for node_id in way.get("nodes", []):
        node = node_index.get(node_id)
        if node:
            coords.append([node["lon"], node["lat"]])
    return coords


def is_closed_ring(coords):
    return len(coords) >= 4 and coords[0] == coords[-1]


def parse_height_value(raw_value):
    if raw_value is None:
        return 0

    value = raw_value.strip().lower().replace("m", "").strip()

    try:
        return float(value)
    except ValueError:
        return 0


def join_ways_into_rings(member_ways, node_index):
    """
    Attempts to stitch relation member ways into closed rings.
    This handles simple multipolygon cases.
    """
    segments = []

    for way in member_ways:
        coords = way_to_coords(way, node_index)
        if len(coords) < 2:
            continue
        segments.append(coords)

    rings = []

    while segments:
        ring = segments.pop(0)

        changed = True
        while changed:
            changed = False
            i = 0
            while i < len(segments):
                seg = segments[i]

                if ring[-1] == seg[0]:
                    ring.extend(seg[1:])
                    segments.pop(i)
                    changed = True
                elif ring[-1] == seg[-1]:
                    ring.extend(reversed(seg[:-1]))
                    segments.pop(i)
                    changed = True
                elif ring[0] == seg[-1]:
                    ring = seg[:-1] + ring
                    segments.pop(i)
                    changed = True
                elif ring[0] == seg[0]:
                    ring = list(reversed(seg[1:])) + ring
                    segments.pop(i)
                    changed = True
                else:
                    i += 1

        if len(ring) >= 4 and ring[0] == ring[-1]:
            rings.append(ring)

    return rings


def relation_rings(relation, way_index, node_index):
    outer_ways = []
    inner_ways = []

    for member in relation.get("members", []):
        if member.get("type") != "way":
            continue

        way = way_index.get(member.get("ref"))
        if not way:
            continue

        role = member.get("role", "")
        if role == "inner":
            inner_ways.append(way)
        else:
            outer_ways.append(way)

    outers = join_ways_into_rings(outer_ways, node_index)
    inners = join_ways_into_rings(inner_ways, node_index)

    return outers, inners


def relation_lines(relation, way_index, node_index):
    lines = []

    for member in relation.get("members", []):
        if member.get("type") != "way":
            continue

        way = way_index.get(member.get("ref"))
        if not way:
            continue

        coords = way_to_coords(way, node_index)
        if len(coords) >= 2:
            lines.append(coords)

    return lines


def write_geojson(filepath, geojson_data):
    with open(filepath, "w", encoding="utf-8") as f:
        json.dump(geojson_data, f, ensure_ascii=False, indent=2)
