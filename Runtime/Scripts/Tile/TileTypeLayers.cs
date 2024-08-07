using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    public static class TileTypeLayers
    {
        public static LayerType GetLayerOfTile(TileType type)
        {
            string typeName = type.ToString();
            if (typeName.StartsWith("Wall"))
            {
                return LayerType.Wall;
            }
            else if (typeName.StartsWith("Ground"))
            {
                return LayerType.Ground;
            }
            else if (typeName.StartsWith("Object"))
            {
                return LayerType.Object;
            }
            else if (typeName.StartsWith("Debug"))
            {
                return LayerType.Debug;
            }
            return LayerType.NA;
        }
    }
}