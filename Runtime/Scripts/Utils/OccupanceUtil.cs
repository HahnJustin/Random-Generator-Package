using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using System.Collections.Generic;

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
                value = tile.ContainsType(config.TileA) ? 1 : 0;
            }
            else if (config.Occupance == OccupanceType.Doors_WO_Not_NA)
            {
                if(tile.ContainsType(TileType.Object_Door)) value = 0;
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
                        tile.Ground = TileType.Ground_Light;
                        break;
                    case LayerType.Wall:
                        tile.Wall = TileType.Wall_Cave;
                        break;
                    case LayerType.Object:
                        tile.Object = TileType.Object_Stalagmite;
                        break;
                    case LayerType.Debug:
                        tile.Debug = TileType.Debug_Star_Red;
                        break;
                    default:
                        break;
                }
            }
            else if (config.Occupance == OccupanceType.Contains_A)
            {
                tile.SetType(config.TileA);
            }
            else
            {
                tile.Wall = TileType.Wall_Cave;
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

        public Vector2Int GetPointInDirection(Vector2Int point, Direction direction, int movement = 1)
        {
            int searchX = point.x;
            int searchY = point.y;
            switch (direction)
            {
                case Direction.Up:
                    searchY += movement;
                    break;
                case Direction.Up_Right:
                    searchX += movement;
                    searchY += movement;
                    break;
                case Direction.Right:
                    searchX += movement;
                    break;
                case Direction.Down_Right:
                    searchX += movement;
                    searchY -= movement;
                    break;
                case Direction.Down:
                    searchY -= movement;
                    break;
                case Direction.Down_Left:
                    searchX -= movement;
                    searchY -= movement;
                    break;
                case Direction.Left:
                    searchX -= movement;
                    break;
                case Direction.Up_Left:
                    searchX -= movement;
                    searchY += movement;
                    break;
            }
            return new Vector2Int(searchX, searchY);
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

    }
}
