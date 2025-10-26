using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using Unity.Mathematics;
using UnityEngine;

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

        public int IsOccupied(int2 position)
        {
            return IsOccupied(position.x, position.y);
        }

        public int IsOccupied(int x, int y)
        {
            if (!tileGrid.IsInBounds(x,y) || !tileGrid.IsInRegion(x, y))
                return OutOfBoundsOccupancy;
            
            int value = 0;
            if (config.Occupance == OccupanceType.Layer_Not_NA)
            {
                value = tileGrid.GetNotEmpty(tileGrid.GetTileId(x, y, (int)config.OccupyLayer));
            }
            else if (config.Occupance == OccupanceType.Contains_A)
            {
                value = tileGrid.ColumnContainsId(x, y, config.TileA) ? 1 : 0;
            }
            else
            {
                value = tileGrid.GetOccupied(x,y);
            }

            if (config.InvertOccupance) return value == 1 ? 0 : 1;
            else return value;
        }

        public bool GetIfOccupiedTileNextToPosition(int2 pos, int movement = 1)
        {
            return GetIfOccupiedTileNextToPosition(pos.x, pos.y, movement);
        }

        public bool GetIfOccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x, y, 1, movement);
        }

        public bool GetIfUnoccupiedTileNextToPosition(int2 pos, int movement = 1)
        {
            return GetIfUnoccupiedTileNextToPosition(pos.x, pos.y, movement);
        }

        public bool GetIfUnoccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x, y, 0, movement);
        }

        public int[,] GetOccupanceGrid()
        {
            int[,] occupanceGrid = new int[tileGrid.width, tileGrid.height];

            foreach(int2 pos in tileGrid.GetPositions())
            {
                occupanceGrid[pos.x, pos.y] = IsOccupied(pos);
            }

            return occupanceGrid;
        }

        public OccupanceData GetOccupanceData()
        {
            return new OccupanceData(config, OutOfBoundsOccupancy);
        }

        public int2 GetNearestUnoccupiedPosition(int2 pos)
        {
            return GetNearestUnoccupiedPosition(pos.x, pos.y);
        }

        public int2 GetNearestUnoccupiedPosition(int x, int y)
        {
            // Iterate through all distances from the center
            for (int d = 1; d < Math.Max(height, width); d++)
            {
                // Check all positions at distance `d`
                for (int dx = -d; dx <= d; dx++)
                {
                    int dy1 = d - Math.Abs(dx); // Top and bottom edges
                    int dy2 = -dy1;

                    // Top edge
                    int x1 = x + dx;
                    int y1 = y + dy1;

                    if (tileGrid.IsInBounds(x1, y1) && IsOccupied(x, y) == 0)
                    {
                        return new int2(x1, y1);
                    }

                    // Bottom edge (avoid duplicate check for middle row)
                    if (dy1 != dy2)
                    {
                        int x2 = x + dx;
                        int y2 = y + dy2;

                        if (tileGrid.IsInBounds(x2, y2) && IsOccupied(x2, y2) == 0)
                        {
                            return new int2(x2, y2);
                        }
                    }
                }
            }
            return Constants.OutsideGridInt2;
        }
    }
}
