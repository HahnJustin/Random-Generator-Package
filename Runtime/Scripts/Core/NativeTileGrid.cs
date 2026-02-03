using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public struct NativeTileGrid : ITileGrid, IDisposable, IEnumerable<ITileColumn>
    {
        public readonly int width;
        public readonly int height;
        public readonly int depth;

        public readonly uint seed;

        private NativeArray<int> tiles;
        private NativeArray<int> values;

        private Unity.Mathematics.Random random;

        // Mask Variables
        private bool masked;
        public bool Masked => masked;
        public ITileMask TileMask
        {
            get => tileMask;
            set => tileMask = (NativeTileMask)value;
        }

        [ReadOnly] internal NativeTileMask tileMask;
        [ReadOnly] private NativeLookupBundle bundle;
        [ReadOnly] private NativeParallelHashSet<int2> excludePositions;
        [ReadOnly] private MetaData metaData;

        // Region Variables
        [ReadOnly] private NativeParallelHashSet<int2> regionPositions;
        private int2 regionMin;
        private int2 regionMax;
        private bool regionLimited;

        // Allocator
        private Allocator allocator;

        // Validity
        public bool IsValid { get; set; }

        public bool IsIncludingTiles
        {
            get
            {
                if (!tileMask.IsValid) return false;
                return tileMask.IsIncludingTiles;
            }
        }

        public bool IsExcludingTiles
        {
            get
            {
                if (!tileMask.IsValid) return false;
                return tileMask.IsExcludingTiles;
            }
        }

        public int2 Minimum => regionMin;
        public int2 Maximum => regionMax;

        private NativeTileGrid(int width, int height, int depth, uint seed, bool allocateCollections)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;

            this.seed = seed;
            random = new(seed);

            allocator = Allocator.Persistent;

            tiles = allocateCollections
                ? new NativeArray<int>(width * height * depth, allocator)
                : default;

            values = allocateCollections
                ? new NativeArray<int>(width * height, allocator)
                : default;

            tileMask = allocateCollections
                ? new NativeTileMask(allocator)
                : default;
            masked = false;

            bundle = default;

            metaData = allocateCollections
               ? new MetaData(allocator)
               : default;

            excludePositions = allocateCollections
                ? new NativeParallelHashSet<int2>(64, allocator)
                : default;

            regionPositions = allocateCollections
                ? new NativeParallelHashSet<int2>(64, allocator)
                : default;

            regionMin = new int2(0, 0);
            regionMax = new int2(width - 1, height - 1);
            regionLimited = false;

            IsValid = true;
        }

        public NativeTileGrid(int width, int height, int layerDepth, uint seed)
            : this(width, height, layerDepth, seed, true)
        {
        }

        public static NativeTileGrid DeepClone(NativeTileGrid other)
        {
            NativeTileGrid grid = new(other.width, other.height, other.depth, other.seed, false);
            Allocator allocator = Allocator.Persistent;

            // Tiles / values
            grid.tiles = other.tiles.DeepClone(allocator);
            grid.values = other.values.DeepClone(allocator);

            // Tile Mask
            grid.masked = other.masked;
            grid.tileMask = other.tileMask.IsValid
                ? (NativeTileMask)other.tileMask.DeepClone()
                : new NativeTileMask(allocator);

            // Lookups (read-only)
            grid.bundle = other.bundle;

            // Meta
            grid.metaData = other.metaData.IsValid
                ? other.metaData.DeepClone()
                : new MetaData(allocator);

            // Exclude Positions
            grid.excludePositions = NativeParallelHashSetCopy(other.excludePositions, allocator);

            // Region Positions
            grid.regionPositions = NativeParallelHashSetCopy(other.regionPositions, allocator);

            // Region bounds
            grid.regionMax = other.regionMax;
            grid.regionMin = other.regionMin;
            grid.regionLimited = other.regionLimited;

            grid.IsValid = true;
            return grid;
        }

        private static NativeParallelHashSet<int2> NativeParallelHashSetCopy(
            NativeParallelHashSet<int2> set,
            Allocator allocator)
        {
            NativeParallelHashSet<int2> newSet =
                new NativeParallelHashSet<int2>(math.ceilpow2(set.Count()), allocator);

            foreach (var pos in set)
                newSet.Add(pos);

            return newSet;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetDefaultOccupanceFromLayerIndex(int layerIndex)
        {
            return bundle.layerIndexToDefaultOccupanceLookup[layerIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetTileKindFromTileId(int id)
        {
            if (bundle.tileIdToTileKindLookup.TryGetValue(id, out int kind))
                return kind;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetEmptyFromTileId(int id) =>
            GetTileKindFromTileId(id) == 1 ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNotEmptyFromTileId(int id) =>
            GetTileKindFromTileId(id) == 1 ? 0 : 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetTileIdFromNativeArray(int3 pos) =>
            tiles[PositionToIndex(pos)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetTileIdFromNativeArray(int x, int y, int z) =>
            tiles[PositionToIndex(x, y, z)];

        private int GetTileIdFromNativeArrayLayerId(int x, int y, int layerId) =>
            tiles[PositionToIndex(x, y, GetLayerIndexFromLayerId(layerId))];

        private int PositionToIndexLayerId(int x, int y, int layerId)
        {
            int z = GetLayerIndexFromLayerId(layerId);
            return PositionToIndex(x, y, z);
        }

        private int PositionToIndexTileId(int x, int y, int tileId)
        {
            int z = GetLayerIndexFromTileId(tileId);
            return PositionToIndex(x, y, z);
        }

        private int PositionToIndex(int x, int y, int z) =>
            depth * (y * width + x) + z;

        private int PositionToIndex(int x, int y) =>
            PositionToIndex(x, y, 0);

        private int PositionToIndex(int3 pos) =>
            PositionToIndex(pos.x, pos.y, pos.z);

        private int PositionToValueIndex(int x, int y) =>
            y * width + x;

        private bool ZInBounds(int z) =>
            z >= 0 && z < depth;

        private bool ZInMetaBounds(int z) =>
            (z >= 0 && z < depth) || z == MetaData.ColumnZ;

        private bool CanModifyPosition(int3 pos)
        {
            if (!Masked) return true;
            int tileId = GetTileIdFromNativeArray(pos);
            return tileMask.CanModifyTileId(tileId);
        }

        private bool CanModifyColumn(int x, int y)
        {
            if (!Masked) return true;
            return tileMask.CanModifyColumn(GetColumnNative(x, y));
        }

        private bool HasTileTable(int id)
        {
            return bundle.tileIdToTableLookup.TryGetValue(id, out TablePointer item);
        }

        private int GetTileIdFromTileTable(int id, int x, int y)
        {
            TablePointer pointer = bundle.tileIdToTableLookup[id];

            // Mix inputs into a well-distributed seed (prevents stripes)
            uint s = (uint)math.hash(new int4(
                (int)seed,
                x * 73856093,
                y * 19349663,
                id * 83492791
            ));
            if (s == 0) s = 1;

            var rng = new Unity.Mathematics.Random(s);
            rng.NextUInt();
            int roll = rng.NextInt(pointer.maxWeight); 

            for (int i = 0; i < pointer.length; i++) 
            {
                int2 entry = bundle.tileTables[pointer.index + i];

                if (roll < entry.y) return entry.x;
                else roll -= entry.y;
            }
            return 0;
        }

        // Exposed Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetLayerIndexFromTileId(int id)
        {
            if (bundle.tileIdToLayerIndexLookup.TryGetValue(id, out int index))
                return index;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetLayerIndexFromLayerId(int id)
        {
            if (bundle.layerIdToLayerIndexLookup.TryGetValue(id, out int index))
                return index;
            return -1;
        }


        // ---------------- ITileGrid primitives ----------------

        // Set Tile Id
        public bool SetTileId(int x, int y, int id)
        {
            if (IsRestricted(x, y) || !CanModifyColumn(x, y)) return false;

            if (HasTileTable(id)) id = GetTileIdFromTileTable(id, x, y);

            // Overwrite ID as zero if id is empty
            if (GetEmptyFromTileId(id) == 1)
                tiles[PositionToIndexTileId(x, y, id)] = 0;
            else
                tiles[PositionToIndexTileId(x, y, id)] = id;

            return true;
        }

        // Force Tile Id (bypass layer rules)
        public bool SetTileIdBypassLayer(int x, int y, int z, int id)
        {
            if (IsRestricted(x, y) || !CanModifyColumn(x, y)) return false;

            tiles[PositionToIndex(x, y, z)] = id;
            return true;
        }

        // Column Contains Id
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ColumnContainsId(int x, int y, int id)
        {
            int z = GetLayerIndexFromTileId(id);   // map tileId -> layer index
            if (z < 0) return false;               // unknown id
            return tiles[PositionToIndex(x, y, z)] == id;
        }

        // Get Tile Id
        public int GetTileId(int x, int y, int layerId)
        {
            if (!IsInBounds(x, y)) return -1;
            return GetTileIdFromNativeArrayLayerId(x, y, layerId);
        }

        public int GetTileIdWithLayerIndex(int x, int y, int z)
        {
            if (!IsInBounds(x, y)) return -1;
            return GetTileIdFromNativeArray(x, y, z);
        }

        // Copy Column
        public bool CopyColumn(int2 replacer, int2 replaced)
        {
            if (!IsInBounds(replaced.x, replaced.y) || !IsInBounds(replacer.x, replacer.y))
                return false;

            for (int i = 0; i < depth; i++)
            {
                int tileId = GetTileIdFromNativeArray(replacer.x, replacer.y, i);
                SetTileId(replaced.x, replaced.y, tileId);
            }

            return true;
        }

        // Get Column
        public ITileColumn GetColumn(int x, int y)
        {
            if (!IsInBounds(x, y)) return default;
            return new NativeTileColumn(
                new NativeSlice<int>(tiles, PositionToIndex(x, y), depth),
                x, y);
        }

        // Internal native column (for masking)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeTileColumn GetColumnNative(int x, int y)
        {
            if (!IsInBounds(x, y)) return default;
            return new NativeTileColumn(
                new NativeSlice<int>(tiles, PositionToIndex(x, y), depth),
                x, y);
        }

        // Set Column
        public bool SetColumn(ITileColumn col)
        {
            if (!IsInBounds(col.X, col.Y) || !col.IsValid) return false;

            for (int z = 0; z < col.Length; z++)
                tiles[PositionToIndex(col.X, col.Y, z)] = col[z];

            return true;
        }

        // Empty helpers
        public int GetEmpty(int tileId) =>
            GetEmptyFromTileId(tileId);

        public int GetNotEmpty(int tileId) =>
            GetNotEmptyFromTileId(tileId);

        public int GetOccupied(int x, int y)
        {
            int index = PositionToIndex(x,y);

            byte forceOccupied = 0;
            byte value = 0;
            for (int z = 0; z < depth; z++)
            {
                int id = tiles[index + z];
                if (id == 0)
                    continue;

                int kind = GetTileKindFromTileId(id);
                if (kind == (int)TileKind.ForceOccupied)
                    forceOccupied = 1;

                if (kind == (int)TileKind.ForceUnoccupied)
                    return 0;

                if (GetDefaultOccupanceFromLayerIndex(z) == 1)
                    value = 1;
            }
            return forceOccupied == 1 ? 1 : value;
        }

        // Tile Value
        public bool SetTileValue(int x, int y, int value)
        {
            if (!IsInBounds(x, y) || !IsInRegion(x, y)) return false;
            values[PositionToValueIndex(x, y)] = value;
            return true;
        }

        public int GetTileValue(int x, int y)
        {
            if (!IsInBounds(x, y)) return 0;
            return values[PositionToValueIndex(x, y)];
        }

        // Meta Data
        public void InitializeMeta(List<FixedString64Bytes> hotMetaKeys) => metaData.Initialize(width, height, hotMetaKeys);
        public int GetMetaIndex(FixedString64Bytes metaKey) => metaData.GetIndex(metaKey);

        public void AddDataLayerId(int x, int y, int layerId, string field, int value) =>
            AddData(x, y, GetLayerIndexFromLayerId(layerId), field, value);

        public void AddData(int x, int y, int layerZ, string field, int value)
        {
            if (!IsInBounds(x, y) || !ZInMetaBounds(layerZ) || !CanModifyColumn(x, y)) return;
            metaData.AddData(new int3(x, y, layerZ), field, value);
        }

        public void AddData(int x, int y, string field, int value) =>
            AddData(x, y, MetaData.ColumnZ, field, value);

        public void AddData(int x, int y, int layerZ, FixedString64Bytes fixedField, int value)
        {
            if (!IsInBounds(x, y) || !ZInMetaBounds(layerZ) || !CanModifyColumn(x, y)) return;
            metaData.AddData(new int3(x, y, layerZ), fixedField, value);
        }

        public void AddData(int x, int y, FixedString64Bytes fixedField, int value) =>
            AddData(x, y, MetaData.ColumnZ, fixedField, value);

        public void AddData(MetadataEntry entry) =>
            AddData(entry.GetShiftedX(), entry.GetShiftedY(), GetLayerIndexFromLayerId(entry.layerId), entry.field, entry.value);

        public void AddData(int2 pos, int fieldIndex, int value)
        {
            if (!IsInBounds(pos.x, pos.y) || !CanModifyColumn(pos.x, pos.y)) return;
            metaData.AddData(pos, fieldIndex, value);
        }

        public int GetDataLayerId(int x, int y, int layerId, string field) =>
            GetData(x, y, GetLayerIndexFromLayerId(layerId), field);

        public int GetData(int x, int y, int layerZ, string field)
        {
            if (!IsInBounds(x, y) || !ZInMetaBounds(layerZ)) return 0;

            if (metaData.TryGetData(new int3(x, y, layerZ), field, out int val))
                return val;

            return 0;
        }

        public int GetData(int x, int y, string field) =>
            GetData(x, y, MetaData.ColumnZ, field);

        public int GetData(int x, int y, int layerZ, FixedString64Bytes fixedField)
        {
            if (!IsInBounds(x, y) || !ZInMetaBounds(layerZ)) return 0;

            if (metaData.TryGetData(new int3(x, y, layerZ), fixedField, out int val))
                return val;

            return 0;
        }

        public int GetData(int x, int y, FixedString64Bytes fixedField) =>
            GetData(x, y, MetaData.ColumnZ, fixedField);

        public int GetData(int2 pos, int fieldIndex)
        {
            if (!IsInBounds(pos.x, pos.y)) return 0;

            if (metaData.TryGetData(pos, fieldIndex, out int val))
                return val;

            return 0;
        }

        public List<MetaPair> GetAllData(int3 pos) =>
            metaData.GetAllData(pos);

        public List<MetaPair> GetAllDataWithColData(int3 pos) =>
            metaData.GetAllDataWithColData(pos);

        public List<PositionValue> GetAllData(FixedString64Bytes fixedField) =>
            metaData.GetAllData(fixedField);

        public List<PositionValue> GetAllData(string field) =>
            metaData.GetAllData(field);

        public int RemoveAllAt(int3 pos) =>
            metaData.RemoveAllAt(pos);

        public List<string> GetMetaFields() => metaData.GetFields();

        // Masking
        public void RemoveMask()
        {
            masked = false;
            tileMask.Dispose();

            Allocator allocator = Allocator.Persistent;
            tileMask = new NativeTileMask(allocator);
        }

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
            tileMask.Dispose();
            tileMask = new(includeList, excludeList, bundle);
        }

        public void ToggleMasked(bool on)
        {
            if (on && !tileMask.IsValid)
                throw new Exception("Attempted to enable masking without a valid TileMask.");
            masked = on;
        }

        public void AddExcludedPosition(int2 pos)
        {
            excludePositions.Add(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsExcluding(int2 pos) =>
            excludePositions.IsCreated && excludePositions.Contains(pos);

        public bool IsInsideMask(int x, int y) =>
            CanModifyColumn(x, y);

        // Region
        public RegionBounds GetRegionBounds()
        {
            var list = new List<int2>(regionPositions.Count());

            foreach (var pos in regionPositions)
                list.Add(pos);

            return new RegionBounds(regionMin, regionMax, list, this);
        }

        public void SetRegionBounds(int2 min, int2 max, List<int2> regionAddPositions)
        {
            regionPositions.Clear();
            regionMin = min;
            regionMax = max;
            regionLimited = true;

            if (regionAddPositions != null)
            {
                for (int i = 0; i < regionAddPositions.Count; i++)
                    regionPositions.Add(regionAddPositions[i]);
            }
        }

        public void SetRegionBounds(RegionBounds regionBounds) =>
            SetRegionBounds(regionBounds.min, regionBounds.max, regionBounds.includingPositions);

        public void RemoveRegion()
        {
            regionMin = new int2(0, 0);
            regionMax = new int2(width - 1, height - 1);
            regionLimited = false;
        }

        public void AddRegionPosition(int x, int y)
        {
            regionPositions.Add(new int2(x, y));
        }

        public bool IsInRegion(int x, int y)
        {
            var pos = new int2(x, y);
            if (!regionLimited) return true;
            return math.all(pos >= regionMin) &&
                   math.all(pos <= regionMax) &&
                   regionPositions.Contains(pos);
        }

        // Bounds / restriction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInBounds(int x, int y) =>
            (uint)x < (uint)width && (uint)y < (uint)height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRestricted(int x, int y)
        {
            // cheapest checks first
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) return true;

            // if no region, skip all region work
            if (regionLimited)
            {
                // bounds check inlined
                if (x < regionMin.x || y < regionMin.y || x > regionMax.x || y > regionMax.y) return true;

                // if regionPositions is empty, treat as "nothing allowed" (or flip to "everything allowed" if you prefer)
                if (!regionPositions.IsCreated || regionPositions.Count() == 0) return true;

                if (!regionPositions.Contains(new int2(x, y))) return true;
            }

            // skip exclude check if unused
            if (excludePositions.IsCreated && excludePositions.Count() > 0)
            {
                if (excludePositions.Contains(new int2(x, y))) return true;
            }

            return false;
        }


        // Iteration / traversal
        public NativeArray<int> AsNativeArray() => tiles;

        public IEnumerable<int2> GetPositions()
        {
            if (regionLimited)
            {
                foreach (var pos in regionPositions)
                    yield return pos;
                yield break;
            }

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    yield return new int2(x, y);
        }

        public IEnumerable<int3> GetPositions3D()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    for (int z = 0; z < depth; z++)
                        yield return new int3(x, y, z);
        }

        public IEnumerable<int4> GetPositionsWithId()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        yield return new int4(x, y, z, GetTileIdFromNativeArray(x, y, z));
                    }
                }
            }
        }

        public IEnumerable<ITileColumn> GetColumns()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    yield return GetColumn(x, y);
        }

        public IEnumerable<int2> GetRegionPositions()
        {
            var na = regionPositions.ToNativeArray(Allocator.Persistent);
            try
            {
                var list = new List<int2>(na.Length);
                for (int i = 0; i < na.Length; i++) list.Add(na[i]);
                return list;
            }
            finally
            {
                na.Dispose();
            }
        }

        public IEnumerable<ITileColumn> GetRegionColumns()
        {
            foreach (int2 pos in GetRegionPositions())
                yield return GetColumn(pos.x, pos.y);
        }

        public IEnumerable<int2> GetRegionGridPositions()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
                for (int x = regionMin.x; x <= regionMax.x; x++)
                    yield return new int2(x, y);
        }

        public IEnumerable<ITileColumn> GetRegionGridColumns()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
                for (int x = regionMin.x; x <= regionMax.x; x++)
                    yield return GetColumn(x, y);
        }

        public IEnumerator<ITileColumn> GetEnumerator()
        {
            if (regionLimited)
                return GetRegionColumns().GetEnumerator();
            return GetColumns().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Lookup bundle
        public void SetLookupBundle(ILookupBundle bundle)
        {
            this.bundle = (NativeLookupBundle)bundle;
        }

        public ILookupBundle GetLookupBundle()
        {
            return bundle;
        }

        // Misc
        public void SetAllTiles(IEnumerable<int> array)
        {
            if (array.Count() != tiles.Length) return;

            int i = 0;
            foreach (int id in array)
            {
                tiles[i] = id;
                i++;
            }
        }

        public SerialTileGrid ToSerial()
        {
            SerialTileGrid grid = new(width, height, depth);
            grid.SetAllTiles(tiles.ToArray());
            grid.tileMask = tileMask.ToSerialMask();
            // TODO: add meta / region / exclude copying if needed
            return grid;
        }

        public void ClearNumbers()
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = 0;
        }

        public void ClearPositiveNumbers()
        {
            for (int i = 0; i < values.Length; i++)
                if (values[i] > 0) values[i] = 0;
        }

        public bool CanHaveTiles() =>
            width > 0 && height > 0 && depth > 0;

        public void Dispose()
        {
            if (!IsValid) return;

            if (tiles.IsCreated) tiles.Dispose();
            if (values.IsCreated) values.Dispose();
            if (tileMask.IsValid) tileMask.Dispose();
            if (metaData.IsValid) metaData.Dispose();
            if (excludePositions.IsCreated) excludePositions.Dispose();
            if (regionPositions.IsCreated) regionPositions.Dispose();

            IsValid = false;
        }
    }
}
