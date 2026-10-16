import json
import itertools
import logging
import sys
import threading
import time
from contextlib import contextmanager

import requests


logger = logging.getLogger(__name__)

OVERPASS_URLS = [
    "https://overpass-api.de/api/interpreter",
    "https://lz4.overpass-api.de/api/interpreter",
    "https://z.overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
]


@contextmanager
def terminal_spinner(message):
    stop_event = threading.Event()
    frames = itertools.cycle("|/-\\")

    def spin():
        while not stop_event.is_set():
            sys.stdout.write(f"\r{message} {next(frames)}")
            sys.stdout.flush()
            stop_event.wait(0.1)

    thread = threading.Thread(target=spin, daemon=True)
    thread.start()
    try:
        yield
    finally:
        stop_event.set()
        thread.join()
        sys.stdout.write("\r" + " " * (len(message) + 2) + "\r")
        sys.stdout.flush()


def query_overpass(query, timeout, max_attempts, retry_delay):
    headers = {"User-Agent": "project-atlas-data-pipeline/0.1"}
    last_error = None

    for endpoint in OVERPASS_URLS:
        for attempt in range(1, max_attempts + 1):
            try:
                with terminal_spinner("Contacting Overpass"):
                    response = requests.post(
                        endpoint,
                        data={"data": query},
                        headers=headers,
                        timeout=timeout,
                    )
                response.raise_for_status()
                return response.json()
            except (requests.exceptions.RequestException, ValueError) as error:
                last_error = error
                logger.warning(
                    "Overpass request failed endpoint=%s attempt=%s/%s error=%s",
                    endpoint,
                    attempt,
                    max_attempts,
                    error,
                )
                if attempt < max_attempts:
                    time.sleep(retry_delay * attempt)

    raise RuntimeError(f"All Overpass endpoints failed. Last error: {last_error}")


def build_node_index(elements):
    return {element["id"]: element for element in elements if element["type"] == "node"}


def build_way_index(elements):
    return {element["id"]: element for element in elements if element["type"] == "way"}


def way_to_coords(way, node_index):
    return [
        [node_index[node_id]["lon"], node_index[node_id]["lat"]]
        for node_id in way.get("nodes", [])
        if node_id in node_index
    ]


def is_closed_ring(coords):
    return len(coords) >= 4 and coords[0] == coords[-1]


def parse_height_value(raw_value):
    if raw_value is None:
        return 0

    try:
        return float(raw_value.strip().lower().replace("m", "").strip())
    except (AttributeError, ValueError):
        return 0


def join_ways_into_rings(member_ways, node_index):
    segments = []
    for way in member_ways:
        coords = way_to_coords(way, node_index)
        if len(coords) >= 2:
            segments.append(coords)
    rings = []

    while segments:
        ring = segments.pop(0)
        changed = True
        while changed:
            changed = False
            index = 0
            while index < len(segments):
                segment = segments[index]
                if ring[-1] == segment[0]:
                    ring.extend(segment[1:])
                elif ring[-1] == segment[-1]:
                    ring.extend(reversed(segment[:-1]))
                elif ring[0] == segment[-1]:
                    ring = segment[:-1] + ring
                elif ring[0] == segment[0]:
                    ring = list(reversed(segment[1:])) + ring
                else:
                    index += 1
                    continue
                segments.pop(index)
                changed = True

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
        if member.get("role") == "inner":
            inner_ways.append(way)
        else:
            outer_ways.append(way)

    return (
        join_ways_into_rings(outer_ways, node_index),
        join_ways_into_rings(inner_ways, node_index),
    )


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
    with open(filepath, "w", encoding="utf-8") as file:
        json.dump(geojson_data, file, ensure_ascii=False, indent=2)


def write_json(filepath, data):
    with open(filepath, "w", encoding="utf-8") as file:
        json.dump(data, file, ensure_ascii=False, indent=2)
