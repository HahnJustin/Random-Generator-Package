using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dalichrome.RandomGenerator.Random;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public class SerialTileGrid : ITileGrid, IEnumerable<ITileColumn>, IDisposable
    {
        public readonly int width;
        public readonly int height;
        public readonly int depth;

        private int[] tiles;
        private int[] values;

        // Mask Variables
        private bool masked;
        public bool Masked => masked;
        public ITileMask TileMask
        {
            get => tileMask;
            set => tileMask = (SerialTileMask)value;
        }

        [ReadOnly] internal SerialTileMask tileMask;
        [ReadOnly] private SerialLookupBundle bundle;
        [ReadOnly] private HashSet<int2> excludePositions;

        // Region Variables
        [ReadOnly] private HashSet<int2> regionPositions;
        private int2 regionMin;
        private int2 regionMax;
        private bool regionLimited;

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

        public SerialTileGrid(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;

            tiles = new int[width * height * depth];
            values = new int[width * height];

            tileMask = new();
            masked = false;

            bundle = default;

            excludePositions = new();
            regionPositions = new();

            regionMin = new int2(0, 0);
            regionMax = new int2(width - 1, height - 1);
            regionLimited = false;

            IsValid = true;
        }

        public static SerialTileGrid DeepClone(SerialTileGrid other)
        {
            SerialTileGrid grid = new(other.width, other.height, other.depth);

            // Tiles / values
            grid.tiles = other.tiles.DeepClone();
            grid.values = other.values.DeepClone();

            // Tile Mask
            grid.masked = other.masked;
            grid.tileMask = other.tileMask.IsValid
                ? (SerialTileMask)other.tileMask.DeepClone()
                : new SerialTileMask
                {
                    includeList = new(other.tileMask.includeList),
                    excludeList = new(other.tileMask.excludeList),
                    IsValid = true
                };

            // Lookups
            grid.bundle = other.bundle;

            // Exclusions / region
            grid.excludePositions = new(other.excludePositions);
            grid.regionPositions = new(other.regionPositions);
            grid.regionMax = other.regionMax;
            grid.regionMin = other.regionMin;
            grid.regionLimited = other.regionLimited;

            grid.IsValid = true;
            return grid;
        }

        private int GetLayerIndexFromTileId(int id)
        {
            if (bundle.tileIdToLayerIndexLookup.TryGetValue(id, out int index))
                return index;
            return -1;
        }

        private int GetLayerIndexFromLayerId(int id)
        {
            if (bundle.layerIdToLayerIndexLookup.TryGetValue(id, out int index))
                return index;
            return -1;
        }

        private int GetTileKindFromTileId(int id)
        {
            if (bundle.tileIdToTileKindLookup.TryGetValue(id, out int kind))
                return kind;
            return -1;
        }

        private int GetEmptyFromTileId(int id) =>
            GetTileKindFromTileId(id) == 1 ? 1 : 0;

        private int GetNotEmptyFromTileId(int id) =>
            GetTileKindFromTileId(id) == 1 ? 0 : 1;

        private int GetDefaultOccupanceFromLayerIndex(int layerIndex) =>
            bundle.layerIndexToDefaultOccupanceLookup[layerIndex];

        private int GetTileIdFromArray(int x, int y, int layerIndex) =>
            tiles[PositionToIndex(x, y, layerIndex)];

        private int GetTileIdFromArrayLayerId(int x, int y, int layerId) =>
            tiles[PositionToIndex(x, y, GetLayerIndexFromLayerId(layerId))];

        private int PositionToIndex(int x, int y, int z) =>
            depth * (y * width + x) + z;

        private int PositionToIndex(int x, int y) =>
            PositionToIndex(x, y, 0);

        private int PositionToValueIndex(int x, int y) =>
            y * width + x;

        private bool CanModifyTile(int3 pos)
        {
            if (!Masked) return true;
            int tileId = tiles[PositionToIndex(pos.x, pos.y, pos.z)];
            return tileMask.CanModifyTileId(tileId);
        }

        private bool CanModifyColumn(int x, int y)
        {
            if (!Masked) return true;

            for (int i = 0; i < depth; i++)
            {
                int tileId = GetTileIdFromArray(x, y, i);
                if (tileMask.CanModifyTileId(tileId))
                    return false;
            }
            return true;
        }

        // ---------------- ITileGrid primitives ----------------

        // Set Tile Id
        public bool SetTileId(int x, int y, int id)
        {
            if (IsRestricted(x, y) || !CanModifyColumn(x, y)) return false;

            tiles[PositionToIndex(x, y, GetLayerIndexFromTileId(id))] = id;
            return true;
        }

        // Force Tile Id
        public bool SetTileIdBypassLayer(int x, int y, int z, int id)
        {
            if (IsRestricted(x, y) || !CanModifyColumn(x, y)) return false;

            tiles[PositionToIndex(x, y, z)] = id;
            return true;
        }

        public bool ColumnContainsId(int x, int y, int id)
        {
            for (int i = 0; i < depth; i++)
            {
                int tileId = GetTileIdFromArray(x, y, i);
                if (tileId == id) return true;
            }
            return false;
        }

        public int GetTileId(int x, int y, int layerId)
        {
            if (!IsInBounds(x, y)) return -1;
            return GetTileIdFromArrayLayerId(x, y, layerId);
        }

        public bool CopyColumn(int2 replacer, int2 replaced)
        {
            if (!IsInBounds(replaced.x, replaced.y) || !IsInBounds(replacer.x, replacer.y))
                return false;

            for (int i = 0; i < depth; i++)
            {
                int tileId = GetTileIdFromArray(replacer.x, replacer.y, i);
                SetTileId(replaced.x, replaced.y, tileId);
            }

            return true;
        }

        // Get Column
        public ITileColumn GetColumn(int x, int y)
        {
            if (!IsInBounds(x, y)) return default;
            return new ManagedTileColumn(tiles, PositionToIndex(x, y), depth, x, y);
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
            -(GetEmptyFromTileId(tileId) - 1);

        public int GetOccupied(int x, int y)
        {
            bool forceUnoccupied = false;
            int value = 0;
            ITileColumn column = GetColumn(x, y);

            for (int z = 0; z < column.Length; z++)
            {
                int id = column[z];
                if (id == 0) continue;

                if (GetTileKindFromTileId(id) == (int)TileKind.ForceOccupied)
                    return 1;

                if (GetTileKindFromTileId(id) == (int)TileKind.ForceUnoccupied)
                    forceUnoccupied = true;

                if (GetDefaultOccupanceFromLayerIndex(z) == 1 &&
                    GetNotEmpty(id) == 1)
                    value = 1;
            }

            return forceUnoccupied ? 0 : value;
        }

        // Value layer
        public bool SetTileValue(int x, int y, int value)
        {
            if (IsRestricted(x, y)) return false;
            values[PositionToValueIndex(x, y)] = value;
            return true;
        }

        public int GetTileValue(int x, int y)
        {
            if (IsRestricted(x, y)) return 0;
            return values[PositionToValueIndex(x, y)];
        }

        // Meta stubs (no-op for serial grid for now)
        public void AddDataLayerId(int x, int y, int layerId, string field, int value) { }
        public void AddData(int x, int y, int layerZ, string field, int value) { }
        public void AddData(int x, int y, string field, int value) { }
        public void AddData(int x, int y, int layerZ, FixedString64Bytes fixedField, int value) { }
        public void AddData(int x, int y, FixedString64Bytes fixedField, int value) { }

        public int GetDataLayerId(int x, int y, int layerId, string field) => 0;
        public int GetData(int x, int y, int layerZ, string field) => 0;
        public int GetData(int x, int y, string field) => 0;
        public int GetData(int x, int y, int layerZ, FixedString64Bytes fixedField) => 0;
        public int GetData(int x, int y, FixedString64Bytes fixedField) => 0;

        public List<MetaPair> GetAllData(int3 pos) => new();
        public List<PositionValue> GetAllData(FixedString64Bytes fixedField) => new();
        public List<PositionValue> GetAllData(string field) => new();

        // Masking
        public void RemoveMask()
        {
            masked = false;
            tileMask = new SerialTileMask
            {
                includeList = new(),
                excludeList = new(),
                IsValid = true
            };
        }

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
            tileMask = new(includeList, excludeList);
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

        public bool IsExcluding(int2 pos) =>
            excludePositions.Contains(pos);

        public bool IsInsideMask(int x, int y) =>
            CanModifyColumn(x, y);

        // Region
        public RegionBounds GetRegionBounds()
        {
            var list = new List<int2>(regionPositions.Count);

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
        public bool IsInBounds(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height;

        public bool IsRestricted(int x, int y) =>
            !IsInBounds(x, y) || IsExcluding(new(x, y)) || !IsInRegion(x, y);

        // Traversal
        public NativeArray<int> AsNativeArray()
        {
            var nativeTiles = new NativeArray<int>(width * height * depth, Allocator.Persistent);
            for (int i = 0; i < nativeTiles.Length; i++)
                nativeTiles[i] = tiles[i];
            return nativeTiles;
        }

        public IEnumerable<int2> GetPositions()
        {
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
                        yield return new int4(x, y, z, GetTileIdFromArray(x, y, z));
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

        public IEnumerable<int2> GetRegionPositions() =>
            regionPositions;

        public IEnumerable<ITileColumn> GetRegionColumns()
        {
            foreach (int2 pos in regionPositions)
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

        public void SetLookupBundle(ILookupBundle bundle)
        {
            this.bundle = (SerialLookupBundle)bundle;
        }

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
            // managed only; nothing to free right now
            IsValid = false;
        }
    }
}
