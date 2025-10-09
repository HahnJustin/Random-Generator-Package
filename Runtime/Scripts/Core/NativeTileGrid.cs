using System.Collections;
using System.Collections.Generic;
using System;
using Dalichrome.RandomGenerator.Random;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;

namespace Dalichrome.RandomGenerator.Core
{
    public struct NativeTileGrid : ITileGrid, IDisposable, IEnumerable
    {
        public readonly int width;
        public readonly int height;
        public readonly int depth;

        private NativeArray<int> tiles;

        // Mask Variables
        private bool masked;
        public bool Masked { get { return masked; } }
        public ITileMask TileMask { get { return tileMask; } set { tileMask = (NativeTileMask)value; } }

        [ReadOnly] internal NativeTileMask tileMask;
        [ReadOnly] private NativeParallelHashMap<int, int> tileIdToLayerIndexLookup;
        [ReadOnly] private NativeParallelHashMap<int, int> layerIdToLayerIndexLookup;
        [ReadOnly] private NativeParallelHashSet<int2> excludePositions;

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

        public int2 Minimum
        {
            get { return regionMin; }
        }

        public int2 Maximum
        {
            get { return regionMax; }
        }

        private NativeTileGrid(int width, int height, int depth, bool allocateCollections)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;

            allocator = Allocator.Persistent;

            tiles = allocateCollections
                ? new NativeArray<int>(width * height * depth, allocator)
                : default;

            tileMask = allocateCollections
                ? new NativeTileMask
                {
                    includeSet = new NativeParallelHashSet<int>(1, allocator),
                    excludeSet = new NativeParallelHashSet<int>(1, allocator),
                    IsValid = true
                }
                : default;
            masked = false;

            tileIdToLayerIndexLookup = allocateCollections
                ? new NativeParallelHashMap<int, int>(depth, allocator)
                : default;

            layerIdToLayerIndexLookup = allocateCollections
                ? new NativeParallelHashMap<int, int>(depth, allocator)
                : default;

            excludePositions = allocateCollections
                ? new NativeParallelHashSet<int2>(64, allocator)
                : default;

            regionPositions = allocateCollections
                ? new NativeParallelHashSet<int2>(64, allocator)
                : default;

            regionMin = new int2(0, 0);
            regionMax = new int2(width-1, height-1);

            regionLimited = false;

            IsValid = true;
        }

        public NativeTileGrid(int width, int height, int layerDepth)
    : this(width, height, layerDepth, true)
        {

        }

        public static NativeTileGrid DeepClone(NativeTileGrid other)
        {
            NativeTileGrid grid = new(other.width, other.height, other.depth, false);

            Allocator allocator = Allocator.Persistent;

            // Tiles
            grid.tiles = other.tiles.DeepClone(allocator);

            // Tile Mask
            grid.masked = other.masked;
            grid.tileMask = other.tileMask.IsValid
                ? (NativeTileMask)other.tileMask.DeepClone()
                : new NativeTileMask
                {
                    includeSet = new NativeParallelHashSet<int>(1, allocator),
                    excludeSet = new NativeParallelHashSet<int>(1, allocator),
                    IsValid = true
                };

            // Lookups
            grid.tileIdToLayerIndexLookup = NativeParallelHashMapCopy(other.tileIdToLayerIndexLookup, allocator);
            grid.tileIdToLayerIndexLookup = NativeParallelHashMapCopy(other.layerIdToLayerIndexLookup, allocator);

            // Exclude Positions
            grid.excludePositions = NativeParallelHashSetCopy(other.excludePositions, allocator);

            // Regional Exclude Positions
            grid.regionPositions = NativeParallelHashSetCopy(other.regionPositions, allocator);

            // Set Region Bounds
            grid.regionMax = other.regionMax;
            grid.regionMin = other.regionMin;

            grid.regionLimited = other.regionLimited;

            grid.IsValid = true;

            return grid;
        }

        private static NativeParallelHashMap<int, int> NativeParallelHashMapCopy(NativeParallelHashMap<int, int> map, Allocator allocator)
        {
            NativeParallelHashMap<int, int> newMap = new NativeParallelHashMap<int, int>(
                math.ceilpow2(map.Count()), allocator);

            foreach (var kvp in map)
                newMap.TryAdd(kvp.Key, kvp.Value);

            return newMap;
        }

        private static NativeParallelHashSet<int2> NativeParallelHashSetCopy(NativeParallelHashSet<int2> set, Allocator allocator)
        {
            NativeParallelHashSet<int2> newSet = new NativeParallelHashSet<int2>(
                math.ceilpow2(set.Count()), allocator);

            foreach (var pos in set)
                newSet.Add(pos);

            return newSet;
        }

        private int GetLayerIndexFromTileId(int id)
        {
            if (tileIdToLayerIndexLookup.TryGetValue(id, out int index))
                return index;

            return -1;
        }

        private int GetLayerIndexFromLayedId(int id)
        {
            if (layerIdToLayerIndexLookup.TryGetValue(id, out int index))
                return index;

            return -1;
        }

        private int GetTileIdFromNativeArray(int3 pos)
        {
            return tiles[PositionToIndex(pos)];
        }

        private int GetTileIdFromNativeArray(int2 pos2, int layerIndex)
        {
            return GetTileIdFromNativeArray(pos2.x, pos2.y, layerIndex);
        }

        private int GetTileIdFromNativeArray(int x, int y, int z)
        {
            return tiles[PositionToIndex(x, y, z)];
        }

        private int GetTileIdFromNativeArrayLayerId(int2 pos2, int layerId)
        {
            return tiles[PositionToIndex(pos2.x, pos2.y, GetLayerIndexFromLayedId(layerId))];
        }

        private int GetTileIdFromNativeArrayLayerId(int x, int y, int layerId)
        {
            return tiles[PositionToIndex(x, y, GetLayerIndexFromLayedId(layerId))];
        }

        private int PositionToIndexLayerId(int x, int y, int layerId)
        {
            int z = GetLayerIndexFromLayedId(layerId);
            return PositionToIndex(x, y, z);
        }

        private int PositionToIndexTileId(int x, int y, int tileId)
        {
            int z = GetLayerIndexFromTileId(tileId);
            return PositionToIndex(x, y, z);
        }

        private int PositionToIndex(int x, int y, int z) => depth * (y * width + x) + z;
        private int PositionToIndex(int x, int y) => PositionToIndex(x, y, 0);
        private int PositionToIndex(int2 position) => PositionToIndex(position.x, position.y, 0);
        private int PositionToIndex(int3 pos) => PositionToIndex(pos.x, pos.y, pos.z);

        private int3 IndexToInt3(int index)
        {
            int tmp = index / depth;
            return new(tmp % width, tmp / width, index % depth);
        }

        private bool CanModifyPosition(int3 pos)
        {
            if (!Masked) return true;

            int tileId = GetTileIdFromNativeArray(pos);
            return tileMask.CanModifyTileId(tileId);
        }

        private bool CanModifyColumn(int2 pos)
        {
            return CanModifyColumn(pos.x, pos.y);
        }

        private bool CanModifyColumn(int x, int y)
        {
            if (!Masked) return true;

            return tileMask.CanModifyColumn(GetColumn(x, y));
        }

        // Set Tile Id
        public bool SetTileId(int x, int y, int id)
        {
            if (IsRestricted(x,y) || !CanModifyColumn(x,y)) return false;

            tiles[PositionToIndexTileId(x,y,id)] = id;
            return true;
        }

        public bool SetTileId(int2 position, int id)
        {
            return SetTileId(position.x, position.y, id);
        }

        // Column Contains Id
        public bool ColumnContainsId(int x, int y, int id)
        {
            TileColumn column = GetColumn(x, y);
            for (int i = 0; i < column.Length; i++)
            {
                if (column[i] == id) return true;
            }
            return false;
        }

        public bool ColumnContainsId(int2 position, int id)
        {
            return ColumnContainsId(position.x, position.y, id);
        }

        // Get Tile Id

        public int GetTileId(int x, int y, int layerId)
        {
            if (!IsInBounds(x, y)) return -1;

            return GetTileIdFromNativeArrayLayerId(x, y, layerId);
        }

        public int GetTileId(int2 position, int layerId)
        {
            return GetTileId(position.x, position.y, layerId);
        }

        // Copy Column
        public bool CopyColumn(int2 replacer, int2 replaced)
        {
            if (!IsInBounds(replaced.x, replaced.y) || !IsInBounds(replacer.x, replacer.y)) return false;

            for (int i = 0; i < depth; i++)
            {
                int tileId = GetTileIdFromNativeArray(replacer, i);
                SetTileId(replaced, tileId);
            }

            return true;
        }

        // Get Column
        public TileColumn GetColumn(int2 position)
        {
            return GetColumn(position.x, position.y);
        }

        public TileColumn GetColumn(int x, int y)
        {
            return TileColumn.FromNative(new NativeSlice<int>(tiles, PositionToIndex(x, y), depth));
        }

        // NA Value stuff
        //Can still set the value for a masked tile
        public bool SetTileValue(int x, int y, int value)
        {
            if (IsRestricted(x, y)) return false;
            
            //TODO - after metadata is coded in

            return true;
        }

        public bool SetTileValue(int2 position, int value)
        {
            return SetTileValue(position.x, position.y, value);
        }

        // Masking

        public void RemoveMask()
        {
            masked = false;
            tileMask.Dispose();

            Allocator allocator = Allocator.Persistent;
            tileMask = new NativeTileMask
            {
                includeSet = new NativeParallelHashSet<int>(1, allocator),
                excludeSet = new NativeParallelHashSet<int>(1, allocator),
                IsValid = true
            };
        }

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
            tileMask.Dispose();
            tileMask = new(includeList, excludeList);
        }

        public void ToggleMasked(bool on)
        {
            if (on && !tileMask.IsValid)
                throw new Exception("Attempted to enable masking without a valid TileMask.");
            masked = on;
        }

        // Persistent / Universal Masking

        public void AddExcludedPosition(int2 position)
        {
            excludePositions.Add(position);
        }

        public bool IsExcluding(int2 position)
        {
            return excludePositions.Contains(position);
        }

        public bool IsExcluding(int x, int y)
        {
            return excludePositions.Contains(new(x,y));
        }

        // Helpers
        public int2 GetNearestPosition(int x, int y, int id)
        {
            // Iterate through all distances from the center
            for (int d = 1; d < Math.Max(height, width); d++)
            {
                // Check all positions at distance `d`
                for (int dx = -d; dx <= d; dx++)
                {
                    int dy1 = d - Math.Abs(dx); // Top and bottom edges
                    int dy2 = -dy1;

                    // Top edge
                    int x1 = x + dx;
                    int y1 = y + dy1;

                    if (IsInBounds(x1, y1) && ColumnContainsId(x,y,id))
                    {
                        return new int2(x1, y1);
                    }

                    // Bottom edge (avoid duplicate check for middle row)
                    if (dy1 != dy2)
                    {
                        int x2 = x + dx;
                        int y2 = y + dy2;

                        if (IsInBounds(x2, y2) && ColumnContainsId(x2, y2, id))
                        {
                            return new int2(x2, y2);
                        }
                    }
                }
            }
            return Constants.OutsideGridInt2;
        }

        public int2 GetNearestPosition(int2 position, int id)
        {
            return GetNearestPosition(position.x, position.y, id);
        }

        public void ClearNumbers()
        {
            // TODO - NA after Metadata changes
        }

        public void ClearPositiveNumbers()
        {
            // TODO - NA after Metadata changes
        }

        private void NeighborPosHelper(int2 pos, List<int2> positions)
        {
            if(IsInBounds(pos)) positions.Add(pos);
        }

        public List<int2> GetEightNeighborPositions(int2 pos)
        {
            List<int2> positions = new();
            NeighborPosHelper(pos + Constants.Int2Left, positions);
            NeighborPosHelper(pos + Constants.Int2Left + Constants.Int2Up, positions);
            NeighborPosHelper(pos + Constants.Int2Up, positions);
            NeighborPosHelper(pos + Constants.Int2Right + Constants.Int2Up, positions);
            NeighborPosHelper(pos + Constants.Int2Right, positions);
            NeighborPosHelper(pos + Constants.Int2Right + Constants.Int2Down, positions);
            NeighborPosHelper(pos + Constants.Int2Down, positions);
            NeighborPosHelper(pos + Constants.Int2Left + Constants.Int2Down, positions);

            return positions;
        }

        public List<int2> GetFourNeighborPositions(int2 pos)
        {
            List<int2> positions = new();
            NeighborPosHelper(pos + Constants.Int2Left, positions);
            NeighborPosHelper(pos + Constants.Int2Up, positions);
            NeighborPosHelper(pos + Constants.Int2Right, positions);
            NeighborPosHelper(pos + Constants.Int2Down, positions);

            return positions;
        }

        public int2 GetRandomEdgePoint(AbstractRandom random)
        {
            int value = random.NextInt(4);

            if (value == 0) return new (width, random.NextInt(height));
            if (value == 1) return new (0, random.NextInt(height));
            if (value == 2) return new (random.NextInt(width), height);
            else return new (random.NextInt(width), 0);
        }

        // Regions
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

            foreach (var pos in regionAddPositions)
                AddRegionPosition(pos);
        }

        public void SetRegionBounds(RegionBounds regionBounds)
        {
            SetRegionBounds(regionBounds.min, regionBounds.max, regionBounds.includingPositions);
        }

        public void RemoveRegion()
        {
            regionMin = new int2(0,0);
            regionMax = new int2(width-1, height-1);
            regionLimited = false;
        }

        public void AddRegionPosition(int x, int y)
        {
            regionPositions.Add(new (x,y));
        }

        public void AddRegionPosition(int2 pos)
        {
            regionPositions.Add(pos);
        }

        // Position Bools
        public bool IsInRegion(int2 pos)
        {
            if (!regionLimited) return true;
            return math.all(pos >= regionMin) &&
                   math.all(pos <= regionMax) &&
                   regionPositions.Contains(pos);
        }

        public bool IsInRegion(int x, int y)
        {
            return IsInRegion(new int2(x,y));
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsInBounds(int2 pos)
        {
            return IsInBounds(pos.x, pos.y);
        }

        public bool IsRestricted(int x, int y)
        {
            return !IsInBounds(x, y) || IsExcluding(x, y) || !IsInRegion(x, y);
        }

        public bool IsRestricted(int2 pos)
        {
            return IsRestricted(pos.x, pos.y);
        }

        public bool IsInsideMask(int2 pos)
        {
            return IsInsideMask(pos.x, pos.y);
        }

        public bool IsInsideMask(int x, int y)
        {
            return CanModifyColumn(x,y);
        }

        // Iteration
        public NativeArray<int> AsNativeArray()
        {
            return tiles;
        }

        public IEnumerable<int2> GetPositions()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    yield return new(x, y);
                }
            }
        }

        public IEnumerable<int3> GetPositions3D()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        yield return new(x, y, z);
                    }
                }
            }
        }

        public IEnumerable<int4> GetPositionsWithId()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        yield return new (x, y, z, GetTileIdFromNativeArray(x,y,z));
                    }
                }
            }
        }

        public IEnumerable<TileColumn> GetColumns()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    yield return GetColumn(x,y);
                }
            }
        }

        public IEnumerable<(int, int, TileColumn)> GetColumnsWithPosition()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    yield return new (x, y, GetColumn(x, y));
                }
            }
        }

        public IEnumerable<int2> GetRegionPositions()
        {
            return regionPositions;
        }

        public IEnumerable<TileColumn> GetRegionColumns()
        {
            foreach (int2 pos in GetRegionPositions())
                yield return GetColumn(pos);
        }

        public IEnumerable<(int, int, TileColumn)> GetRegionColumnsWithPosition()
        {
            foreach (int2 pos in GetRegionPositions())
                yield return new(pos.x, pos.y, GetColumn(pos));
        }

        public IEnumerable<int2> GetRegionGridPositions()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
            {
                for (int x = regionMin.x; x <= regionMax.x; x++)
                {
                    yield return new (x, y);
                }
            }
        }

        public IEnumerable<TileColumn> GetRegionGridColumns()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
            {
                for (int x = regionMin.x; x <= regionMax.x; x++)
                {
                    yield return GetColumn(x, y);
                }
            }
        }

        public IEnumerable<(int, int, TileColumn)> GetRegionGridColumnsWithPosition()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
            {
                for (int x = regionMin.x; x <= regionMax.x; x++)
                {
                    yield return new (x, y, GetColumn(x, y));
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (regionLimited)
            {
                return GetRegionColumnsWithPosition().GetEnumerator();
            }
            else return GetColumnsWithPosition().GetEnumerator();
        }

        // Setting Lookups
        public void SetTileIdToLayerIndexLookup(IReadOnlyDictionary<int, int> newLookup)
        {
            var map = new NativeParallelHashMap<int, int>(newLookup.Count, allocator);
            foreach (var kv in newLookup) map.TryAdd(kv.Key, kv.Value);
            tileIdToLayerIndexLookup = map;
        }
        public void SetLayerIdToLayerIndexLookup(IReadOnlyDictionary<int, int> newLookup)
        {
            var map = new NativeParallelHashMap<int, int>(newLookup.Count, allocator);
            foreach (var kv in newLookup) map.TryAdd(kv.Key, kv.Value);
            layerIdToLayerIndexLookup = map;
        }

        // Misc
        public void SetAllTiles(IEnumerable<int> array)
        {
            if (array.Count() != tiles.Count()) return;

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

            //TODO finish adding other stuff here later

            return grid;
        }

        public bool CanHaveTiles()
        {
            return width > 0 && height > 0 && depth > 0;
        }

        public void Dispose()
        {
            if (!IsValid) return;

            tiles.Dispose();
            tileMask.Dispose();

            tileIdToLayerIndexLookup.Dispose();
            layerIdToLayerIndexLookup.Dispose();
            excludePositions.Dispose();
            regionPositions.Dispose();

            IsValid = false;
        }
    }
}