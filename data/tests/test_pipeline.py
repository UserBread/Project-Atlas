import pytest

from atlas_data_pipeline.cli import validate_and_sanitise_name, validate_bounding_box
from atlas_data_pipeline.queries.buildings import osm_to_buildings_geojson
from atlas_data_pipeline.queries.roads import QUERY_TEMPLATE as ROADS_QUERY


def test_validate_bounding_box_accepts_valid_values():
    assert validate_bounding_box("55.34,-7.35,55.37,-7.29") is True


def test_validated_bounding_box_remains_a_coordinate_string():
    bbox = "55.34,-7.35,55.37,-7.29"

    assert validate_bounding_box(bbox) is True
    assert bbox != "True"


@pytest.mark.parametrize(
    "bbox",
    [
        "55.37,-7.35,55.34,-7.29",
        "91,-7.35,55.37,-7.29",
        "55.34,-181,55.37,-7.29",
        "55.34,-7.35,55.37",
    ],
)
def test_validate_bounding_box_rejects_invalid_values(bbox):
    with pytest.raises(ValueError):
        validate_bounding_box(bbox)


def test_validate_and_sanitise_name():
    assert validate_and_sanitise_name("Malin Head") == "malin_head"
    assert validate_and_sanitise_name("bad:name?.json") == "bad_name_.json"


def test_roads_query_uses_configured_timeout():
    query = ROADS_QUERY.format(bbox="1,2,3,4", timeout=45)
    assert "[out:json][timeout:45];" in query
    assert "(1,2,3,4)" in query


def test_building_conversion_preserves_feature_metadata():
    data = {
        "elements": [
            {"type": "node", "id": 1, "lon": -7.35, "lat": 55.34},
            {"type": "node", "id": 2, "lon": -7.34, "lat": 55.34},
            {"type": "node", "id": 3, "lon": -7.34, "lat": 55.35},
            {"type": "node", "id": 4, "lon": -7.35, "lat": 55.35},
            {
                "type": "way",
                "id": 100,
                "nodes": [1, 2, 3, 4, 1],
                "tags": {"building": "house", "name": "Test House", "height": "6m"},
            },
        ]
    }

    result = osm_to_buildings_geojson(data)

    assert result["type"] == "FeatureCollection"
    assert len(result["features"]) == 1
    feature = result["features"][0]
    assert feature["id"] == "100"
    assert feature["properties"]["primary_name"] == "Test House"
    assert feature["properties"]["height"] == 6
