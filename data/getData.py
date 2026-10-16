import json
import re
from pathlib import Path


def validate_bounding_box(bbox):
    try:
        values = [part.strip() for part in bbox.split(",")]
        if len(values) != 4:
            raise ValueError("Expected south, west, north, east.")

        south, west, north, east = map(float, values)
        if not (
            -90 <= south <= 90 and 
            -90 <= north <= 90 and
            -180 <= west <= 180 and
            -180 <= east <= 180
        ):
            raise ValueError("Coordinates out of valid range.")

        if south >= north or west >= east:
            raise ValueError("South must be less than north and west must be less than east.")

        return True
    except (AttributeError, TypeError, ValueError) as error:
        print(f"Invalid bounding box: {error}")
        return False


def validate_and_sanitize_name(name):
    if not isinstance(name, str) or not name.strip():
        raise ValueError("Invalid name: must be a non-empty string.")

    sanitized = re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", name.strip())
    sanitized = re.sub(r"\s+", "_", sanitized).strip(" ._").lower()

    if not sanitized:
        raise ValueError("Invalid name: no usable filename characters.")

    return sanitized

def main():
    config_path = Path(__file__).with_name("config.json")
    with config_path.open("r", encoding="utf-8") as file:
        config = json.load(file)

    location_id = "malin_Head"
    try:
        location = config["locations"][location_id]
    except KeyError as error:
        raise SystemExit(f"Location '{location_id}' was not found in config.json") from error

    try:
        bounding_box = ",".join(str(location[key]) for key in ("south", "west", "north", "east"))
        filename = validate_and_sanitize_name(location["slug"])
    except (KeyError, TypeError, ValueError) as error:
        raise SystemExit(f"Invalid configuration for '{location_id}': {error}") from error

    if not validate_bounding_box(bounding_box):
        raise SystemExit(1)

    print(f"Location: {location.get('name', location_id)}")
    print(f"Filename prefix: {filename}")
    print(f"Bounding box: {bounding_box}")

if __name__ == "__main__":
    main()