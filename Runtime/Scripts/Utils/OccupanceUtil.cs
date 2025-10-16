using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;

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
            if (!tileGrid.IsInBounds(x,y))
                return OutOfBoundsOccupancy;
            
            ITileColumn col = tileGrid.GetColumn(x, y);

            if (!tileGrid.IsInRegion(x, y))
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
    }
}
