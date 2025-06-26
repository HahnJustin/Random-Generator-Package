using Unity.Mathematics;
using Unity.Collections;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator;

namespace Dalichrome.RandomGenerator.Generators
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

            Tile tile = grid.GetTile(pos.x, pos.y);
            int value;

            switch (occupanceType)
            {
                case OccupanceType.Layer_Not_NA:
                    value = tile.GetOccupied(occupyLayer);
                    break;
                case OccupanceType.Contains_A:
                    value = tile.ContainsId(tileA) ? 1 : 0;
                    break;
                case OccupanceType.Doors_WO_Not_NA:
                    value = tile.ContainsId((int)TileType.Object_Door) ? 0 : tile.GetOccupied();
                    break;
                default:
                    value = tile.GetOccupied();
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