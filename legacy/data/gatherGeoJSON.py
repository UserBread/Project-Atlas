import os
import Queries.buildings as buildings
import Queries.roads as roads
import Queries.footpaths as footpaths

# Bounding box coords: south, west, north, east
bby_min = 55.34  #1
bbx_min = -7.35  #2
bby_max = 55.37  #3
bbx_max = -7.29  #4
city = "malinHead"

bbox = f"{bby_min},{bbx_min},{bby_max},{bbx_max}"

script_dir = os.path.dirname(os.path.abspath(__file__))
output_dir = os.path.abspath(os.path.join(script_dir, "..", "My project", "Assets", "StreamingAssets"))
os.makedirs(output_dir, exist_ok=True)


def main():
    buildings.run(bbox, city, output_dir)
    roads.run(bbox, city, output_dir)
    footpaths.run(bbox, city, output_dir)


if __name__ == "__main__":
    main()