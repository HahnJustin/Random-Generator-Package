using Unity.Mathematics;
using Unity.Collections;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Utils
{
    public struct OccupanceData
    {
        public OccupanceType occupanceType;
        public LayerType occupyLayer;
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
            if (!grid.IsInBounds(pos))
                return outOfBoundsValue;

            int value;

            switch (occupanceType)
            {
                case OccupanceType.Layer_Not_NA:
                    value = grid.GetNotEmptyAt(pos, (int)occupyLayer);
                    break;
                case OccupanceType.Contains_A:
                    value = grid.ColumnContainsId(pos,tileA) ? 1 : 0;
                    break;
                // TODO add new type that can have multiple layers then check all configged layers
                default:
                    value = grid.GetOccupied(pos);
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