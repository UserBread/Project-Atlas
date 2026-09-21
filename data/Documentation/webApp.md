# Gathering data from the web-based application

Navigate to [Overpass Turbo](https://overpass-turbo.eu), paste one of the queries below, click **Run**, then use **Export → GeoJSON** to download the result.

After which, place those GeoJSON files into `My project/Assets/StreamingAssets/`

The bounding box format is `south, west, north, east`.

**Note:** replace south, west, north, east with the respective values.

## Buildings Query

```
[out:json][timeout:30];
(
  way["building"](south, west, north, east);
  relation["building"]["type"="multipolygon"](south, west, north, east);
);
out body;
>;
out skel qt;
```

## Roads Query

```
[out:json][timeout:30];
(
  way["highway"~"motorway|trunk|primary|secondary|tertiary|unclassified|residential|service|living_street|road"](south, west, north, east);
);
out body;
>;
out skel qt;
```

## Footpaths Query

```
[out:json][timeout:30];
(
  way["highway"~"footway|pedestrian|steps"](south, west, north, east);
  way["highway"="path"]["foot"~"yes|designated"](south, west, north, east);
  relation["highway"~"footway|pedestrian|steps"](south, west, north, east);
);
out body;
>;
out skel qt;
```