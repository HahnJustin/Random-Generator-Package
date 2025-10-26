using System.Collections;
using System.Collections.Generic;
using Unity.Collections;


namespace Dalichrome.RandomGenerator
{
    public static class LookupBundleBuilder {

        private static Allocator allocator = Allocator.Persistent;

        private static NativeLookupBundle currentNativeBundle;
        private static SerialLookupBundle currentSerialBundle;

        private static NativeParallelHashMap<int,int> ToNative(IReadOnlyDictionary<int,int> dict)
        {
            var map = new NativeParallelHashMap<int, int>(dict.Count, allocator);
            foreach (var kv in dict) map.TryAdd(kv.Key, kv.Value);
            return map;
        }

        private static NativeArray<int> ToNative(IReadOnlyList<int> list)
        {
            var array = new NativeArray<int>(list.Count, allocator);
            for (int i = 0; i < list.Count; i++) 
                array[i] = list[i];
            return array;
        }

        public static NativeLookupBundle GetNative()
        {
            if(currentNativeBundle.valid == 1) return currentNativeBundle;

            currentNativeBundle = new NativeLookupBundle
            {
                tileIdToLayerIndexLookup = ToNative(TileObjectInfo.TileIdToLayerZ),
                layerIdToLayerIndexLookup = ToNative(TileLayerInfo.LayerIdToZ),
                tileIdToTileKindLookup = ToNative(TileObjectInfo.TileKindByTileIdInt),
                layerIndexToDefaultOccupanceLookup = ToNative(TileLayerInfo.ZToDefaultOccupance),
                valid = 1
            };
            return currentNativeBundle;
        }

        public static SerialLookupBundle GetSerial()
        {
            if (currentSerialBundle.valid == 1) return currentSerialBundle;

            currentSerialBundle = new SerialLookupBundle
            {
                tileIdToLayerIndexLookup = TileObjectInfo.TileIdToLayerZ,
                layerIdToLayerIndexLookup = TileLayerInfo.LayerIdToZ,
                tileIdToTileKindLookup = TileObjectInfo.TileKindByTileIdInt,
                layerIndexToDefaultOccupanceLookup = TileLayerInfo.ZToDefaultOccupance,
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