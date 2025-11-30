using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace Dalichrome.RandomGenerator.Utils
{
    public struct OccupanceData
    {
        public OccupanceType occupanceType;
        public int occupyLayer;
        public int tileA;
        public bool invert;
        public int outOfBoundsValue;

        public OccupanceData(IOccupanceConfig config, int outOfBounds = 0)
        {
            occupanceType = config.Occupance;
            occupyLayer = config.OccupyLayer;
            tileA = config.TileA;
            invert = config.InvertOccupance;
            outOfBoundsValue = outOfBounds;
        }

        public int IsOccupied(int2 pos, NativeTileGrid grid)
        {
            if (!grid.IsInBounds(pos.x, pos.y))
                return outOfBoundsValue;

            int value;

            switch (occupanceType)
            {
                case OccupanceType.Layer_Not_NA:
                    value = grid.GetNotEmpty(grid.GetTileId(pos.x, pos.y, occupyLayer));
                    break;
                case OccupanceType.Contains_A:
                    value = grid.ColumnContainsId(pos.x, pos.y, tileA) ? 1 : 0;
                    break;
                // TODO add new type that can have multiple layers then check all configged layers
                default:
                    value = grid.GetOccupied(pos.x, pos.y);
                    break;
            }

            return invert ? 1 - value : value;
        }

        public void SetInvert(bool _invert)
        {
            invert = _invert;
        }
    }
}