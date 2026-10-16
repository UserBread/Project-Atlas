import argparse
import json
import logging
import re
from pathlib import Path

from .queries import buildings, footpaths, roads


logger = logging.getLogger(__name__)

def load_config(config_path):
    with config_path.open("r", encoding="utf-8") as file:
        return json.load(file)

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
        raise ValueError(f"Invalid bounding box: {error}") from error


def validate_and_sanitise_name(name):
    if not isinstance(name, str) or not name.strip():
        raise ValueError("Invalid name: must be a non-empty string.")

    sanitised = re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", name.strip())
    sanitised = re.sub(r"\s+", "_", sanitised).strip(" ._").lower()

    if not sanitised:
        raise ValueError("Invalid name: no usable filename characters.")

    return sanitised

def resolve_output_dir(output_config, config_path, key, fallback):
        output_dir = Path(output_config.get(key, fallback))
        if not output_dir.is_absolute():
            output_dir = config_path.parent / output_dir
        try:
            output_dir.mkdir(parents=True, exist_ok=True)
        except OSError as error:
            raise SystemExit(f"Could not create output directory '{output_dir}': {error}") from error
        return output_dir

def main():
    logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")

    parser = argparse.ArgumentParser(description="Generate Atlas OSM datasets")
    parser.add_argument("--config", type=Path, default=Path("config.json"))
    parser.add_argument("--location", default="malin_Head")
    args = parser.parse_args()

    config_path = args.config.resolve()
    try:
        config = load_config(config_path)
    except FileNotFoundError as error:
        raise SystemExit(f"Configuration file was not found: {config_path}") from error
    except json.JSONDecodeError as error:
        raise SystemExit(f"Configuration file is not valid JSON: {error}") from error
    except OSError as error:
        raise SystemExit(f"Could not read configuration file: {error}") from error

    try:
        location = config["locations"][args.location]
        bbox = ",".join(str(location[key]) for key in ("south", "west", "north", "east"))
        validate_bounding_box(bbox)
        slug = validate_and_sanitise_name(location["slug"])
    except (KeyError, TypeError, ValueError) as error:
        raise SystemExit(f"Invalid configuration: {error}") from error

    defaults = config.get("default", {})
    source = str(defaults.get("source", "overpass")).lower()
    if source != "overpass":
        raise SystemExit(
            f"Unsupported data source '{source}'. Only 'overpass' is available."
        )

    retry_config = defaults.get("retry", {})

    timeout = defaults.get("timeout", 180)
    max_attempts = retry_config.get("max_attempts", 2)
    retry_delay = retry_config.get("wait_time", 2)

    try:
        timeout = float(timeout)
        max_attempts = int(max_attempts)
        retry_delay = float(retry_delay)
    except (TypeError, ValueError) as error:
        raise SystemExit(f"Invalid retry configuration: {error}") from error

    if timeout <= 0 or max_attempts < 1 or retry_delay < 0:
        raise SystemExit(
            "Invalid retry configuration: timeout must be positive, "
            "max_attempts must be at least 1, and wait_time cannot be negative"
        )

    output_config = defaults.get("output_dir", {})

    raw_output_dir = resolve_output_dir(output_config, config_path, "raw", "raw")
    processed_output_dir = resolve_output_dir(output_config, config_path, "processed", "processed")

    logger.info("Processing location=%s source=%s", args.location, source)
    try:
        buildings.run(
            bbox,
            slug,
            str(processed_output_dir),
            str(raw_output_dir),
            timeout,
            max_attempts,
            retry_delay,
        )
        roads.run(
            bbox,
            slug,
            str(processed_output_dir),
            str(raw_output_dir),
            timeout,
            max_attempts,
            retry_delay,
        )
        footpaths.run(
            bbox,
            slug,
            str(processed_output_dir),
            str(raw_output_dir),
            timeout,
            max_attempts,
            retry_delay,
        )
    except (OSError, RuntimeError, ValueError) as error:
        raise SystemExit(f"Data pipeline failed: {error}") from error


if __name__ == "__main__":
    main()
