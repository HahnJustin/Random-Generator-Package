using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Utils
{
    public class OccupanceUtil : AbstractUtil, IOccupanceUtil
    {
        protected new IOccupanceConfig config;

        public int OutOfBoundsOccupancy { get; set; }


        public OccupanceUtil(IOccupanceConfig config) : base((AbstractConfig)config)
        {
            this.config = config;
        }

        private bool GetIfTileNextToPositionHelper(int x, int y, int occupiedVal, int movement = 1)
        {

            return IsOccupied(x - movement, y) == occupiedVal || // Check left
                   IsOccupied(x - movement, y - movement) == occupiedVal || // Check down left
                   IsOccupied(x, y - movement) == occupiedVal || // Check down
                   IsOccupied(x + movement, y - movement) == occupiedVal || // Check down right
                   IsOccupied(x + movement, y) == occupiedVal || // Check right
                   IsOccupied(x + movement, y + movement) == 1 || // Check Up Right
                   IsOccupied(x, y + movement) == occupiedVal || // Check up
                   IsOccupied(x - movement, y + movement) == occupiedVal; //Check up left
        }

        public int IsOccupied(Vector2Int position)
        {
            return IsOccupied(position.x, position.y);
        }

        public int IsOccupied(int x, int y)
        {
            if (width <= x || x < 0 || height <= y || y < 0)
            {
                return OutOfBoundsOccupancy;
            }

            Tile tile = tileGrid.GetTile(x, y);
            return IsOccupied(tile);
        }

        public int IsOccupied(Tile tile)
        {
            int value = 0;
            if (config.Occupance == OccupanceType.Layer_Not_NA)
            {
                value = tile.GetOccupied(config.OccupyLayer);
            }
            else if (config.Occupance == OccupanceType.Contains_A)
            {
                value = tile.ContainsId((int)config.TileA) ? 1 : 0;
            }
            else if (config.Occupance == OccupanceType.Doors_WO_Not_NA)
            {
                if(tile.ContainsId((int)TileType.Object_Door)) value = 0;
                else value = tile.GetOccupied();
            }
            else
            {
                value = tile.GetOccupied();
            }

            if (config.InvertOccupance) return value == 1 ? 0 : 1;
            else return value;
        }

        public void Fill(Tile tile)
        {
            if (config.Occupance == OccupanceType.Layer_Not_NA)
            {
                switch (config.OccupyLayer)
                {
                    case LayerType.Ground:
                        tileGrid.SetTileId(tile, (int)TileType.Ground_Light);
                        break;
                    case LayerType.Wall:
                        tileGrid.SetTileId(tile, (int)TileType.Wall_Cave);
                        break;
                    case LayerType.Object:
                        tileGrid.SetTileId(tile, (int)TileType.Object_Stalagmite);
                        break;
                    case LayerType.Debug:
                        tileGrid.SetTileId(tile, (int)TileType.Debug_Star_Red);
                        break;
                    default:
                        break;
                }
            }
            else if (config.Occupance == OccupanceType.Contains_A)
            {
                tileGrid.SetTileId(tile, config.TileA);
            }
            else
            {
                tileGrid.SetTileId(tile, (int)TileType.Wall_Cave);
            }
        }

        public bool GetIfOccupiedTileNextToPosition(Tile tile, int movement = 1)
        {
            return GetIfOccupiedTileNextToPosition(tile.x, tile.y, movement);
        }

        public bool GetIfOccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x, y, 1, movement);
        }

        public bool GetIfUnoccupiedTileNextToPosition(Tile tile, int movement = 1)
        {
            return GetIfUnoccupiedTileNextToPosition(tile.x, tile.y, movement);
        }

        public bool GetIfUnoccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x, y, 0, movement);
        }

        public int[,] GetOccupanceGrid()
        {
            int[,] occupanceGrid = new int[tileGrid.width, tileGrid.height];

            foreach(Tile tile in tileGrid)
            {
                occupanceGrid[tile.x, tile.y] = IsOccupied(tile);
            }

            return occupanceGrid;
        }

        public OccupanceData GetOccupanceData()
        {
            return new OccupanceData(config, OutOfBoundsOccupancy);
        }
    }
}
