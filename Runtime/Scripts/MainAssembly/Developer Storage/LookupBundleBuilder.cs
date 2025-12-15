using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;


namespace Dalichrome.RandomGenerator
{
    public static class LookupBundleBuilder {

        private static Allocator allocator = Allocator.Persistent;

        private static NativeLookupBundle currentNativeBundle;
        private static SerialLookupBundle currentSerialBundle;

        private static NativeParallelHashMap<int, T> ToNative<T>(
            IReadOnlyDictionary<int, T> dict)
            where T : unmanaged
        {
            var map = new NativeParallelHashMap<int, T>(dict.Count, allocator);
            foreach (var kv in dict)
                map.TryAdd(kv.Key, kv.Value);
            return map;
        }

        private static NativeArray<T> ToNative<T>(
            IReadOnlyList<T> list)
            where T : unmanaged
        {
            var array = new NativeArray<T>(list.Count, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < list.Count; i++)
                array[i] = list[i];
            return array;
        }

        public static NativeLookupBundle GetNative()
        {
            if(currentNativeBundle.valid == 1) return currentNativeBundle;

            currentNativeBundle = new NativeLookupBundle
            {
                tileIdToLayerIndexLookup = ToNative(TileObjectRegistry.TileIdToLayerZ),
                layerIdToLayerIndexLookup = ToNative(TileLayerRegistry.LayerIdToZ),
                tileIdToTileKindLookup = ToNative(TileObjectRegistry.TileKindByTileIdInt),
                layerIndexToDefaultOccupanceLookup = ToNative(TileLayerRegistry.ZToDefaultOccupance),
                tileIdToTableLookup = ToNative(TileObjectRegistry.TileIdToTablePointer),
                tileTables = ToNative(TileObjectRegistry.TileTables),
                valid = 1
            };
            return currentNativeBundle;
        }

        public static SerialLookupBundle GetSerial()
        {
            if (currentSerialBundle.valid == 1) return currentSerialBundle;

            currentSerialBundle = new SerialLookupBundle
            {
                tileIdToLayerIndexLookup = TileObjectRegistry.TileIdToLayerZ,
                layerIdToLayerIndexLookup = TileLayerRegistry.LayerIdToZ,
                tileIdToTileKindLookup = TileObjectRegistry.TileKindByTileIdInt,
                layerIndexToDefaultOccupanceLookup = TileLayerRegistry.ZToDefaultOccupance,
                tileIdToTableLookup = TileObjectRegistry.TileIdToTablePointer,
                tileTables = TileObjectRegistry.TileTables,
                valid = 1
            };
            return currentSerialBundle;
        }

        public static void DisposeCachedNativeBundle()
        {
            if (currentNativeBundle.valid == 1) currentNativeBundle.Dispose();
            currentNativeBundle = default;
        }

        public static void InvalidateSerial() => currentSerialBundle.valid = 0;
    }
}