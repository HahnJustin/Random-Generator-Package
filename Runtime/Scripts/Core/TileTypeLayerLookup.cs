using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Dalichrome.RandomGenerator.Core
{
    public static class TileTypeLayerLookup
    {
        public static NativeParallelHashMap<int, LayerType> CreateLookup(Allocator allocator)
        {
            var map = new NativeParallelHashMap<int, LayerType>(64, allocator); // Initial capacity

            void Set(TileType type, LayerType layer) => map.TryAdd((int)type, layer);

            // NA
            Set(TileType.NA, LayerType.NA);

            // WALL
            Set(TileType.Wall_Cave, LayerType.Wall);
            Set(TileType.Wall_Cave_Light, LayerType.Wall);
            Set(TileType.Wall_Cobble, LayerType.Wall);
            Set(TileType.Wall_Sandy, LayerType.Wall);
            Set(TileType.Wall_NA, LayerType.Wall);
            Set(TileType.Wall_Object_NA, LayerType.Wall);

            // GROUND
            Set(TileType.Ground_Light, LayerType.Ground);
            Set(TileType.Ground_Dark, LayerType.Ground);
            Set(TileType.Ground_Grass, LayerType.Ground);
            Set(TileType.Ground_Water, LayerType.Ground);
            Set(TileType.Ground_Shallow_Water, LayerType.Ground);
            Set(TileType.Ground_Cobble, LayerType.Ground);
            Set(TileType.Ground_NA, LayerType.Ground);

            // OBJECT
            Set(TileType.Object_Ore_Iron, LayerType.Object);
            Set(TileType.Object_Ore_Crystal, LayerType.Object);
            Set(TileType.Object_Stalagmite, LayerType.Object);
            Set(TileType.Object_Palm_Tree, LayerType.Object);
            Set(TileType.Object_Palm_Tree_Large, LayerType.Object);
            Set(TileType.Object_Sack_Grub, LayerType.Object);
            Set(TileType.Object_Bush_Dead, LayerType.Object);
            Set(TileType.Object_Entrance, LayerType.Object);
            Set(TileType.Object_Exit, LayerType.Object);
            Set(TileType.Object_Door, LayerType.Object);
            Set(TileType.Object_Grass_Patch, LayerType.Object);
            Set(TileType.Object_Grass_Tall, LayerType.Object);
            Set(TileType.Object_NA, LayerType.Object);

            // DEBUG
            Set(TileType.Debug_Star_Blue, LayerType.Debug);
            Set(TileType.Debug_Star_Red, LayerType.Debug);
            Set(TileType.Debug_Star_Green, LayerType.Debug);
            Set(TileType.Debug_Star_Yellow, LayerType.Debug);
            Set(TileType.Debug_Circle_Blue, LayerType.Debug);
            Set(TileType.Debug_Circle_Red, LayerType.Debug);
            Set(TileType.Debug_Circle_Green, LayerType.Debug);
            Set(TileType.Debug_Circle_Yellow, LayerType.Debug);
            Set(TileType.Debug_Path_Blue, LayerType.Debug);
            Set(TileType.Debug_Path_Red, LayerType.Debug);
            Set(TileType.Debug_Path_Green, LayerType.Debug);
            Set(TileType.Debug_Path_Yellow, LayerType.Debug);
            Set(TileType.Debug_Uneditable, LayerType.Debug);
            Set(TileType.Debug_Copy, LayerType.Debug);
            Set(TileType.Debug_NA, LayerType.Debug);
            Set(TileType.Debug_Technical, LayerType.Debug);
            Set(TileType.Debug_Technical2, LayerType.Debug);

            return map;
        }
    }
}