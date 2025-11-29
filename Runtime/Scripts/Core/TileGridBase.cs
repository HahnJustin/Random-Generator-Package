using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    internal abstract class TileGridBase : ITileGrid, IEnumerable<ITileColumn>
    {
        // Core dimensions
        protected readonly int width;
        protected readonly int height;
        protected readonly int depth;

        // Region state
        protected int2 regionMin;
        protected int2 regionMax;
        protected bool regionLimited;

        // Validity
        public bool IsValid { get; set; }

        public int2 Minimum => regionMin;
        public int2 Maximum => regionMax;

        // Masking & lookup are still backend-specific
        public abstract bool Masked { get; }
        public abstract ITileMask TileMask { get; set; }
        public abstract bool IsIncludingTiles { get; }
        public abstract bool IsExcludingTiles { get; }

        protected TileGridBase(int width, int height, int depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;

            regionMin = new int2(0, 0);
            regionMax = new int2(width - 1, height - 1);
            regionLimited = false;

            IsValid = true;
        }

        // ============================================================
        // --------- ABSTRACT PRIMITIVES (to be implemented) ----------
        // ============================================================

        // Tile id / column primitives
        public abstract bool SetTileId(int x, int y, int id);
        public bool SetTileId(int2 position, int id) => SetTileId(position.x, position.y, id);

        public abstract bool SetTileIdBypassLayer(int x, int y, int z, int value);
        public bool SetTileIdBypassLayer(int3 position, int value) => SetTileIdBypassLayer(position.x, position.y, position.z, value);

        public abstract bool CopyColumn(int2 replacer, int2 replaced);

        public abstract ITileColumn GetColumn(int x, int y);
        public ITileColumn GetColumn(int2 pos) => GetColumn(pos.x, pos.y);

        public abstract bool SetColumn(ITileColumn col);

        public abstract int GetEmpty(int id);

        public abstract bool ColumnContainsId(int x, int y, int id);
        public bool ColumnContainsId(int2 position, int id) => ColumnContainsId(position.x, position.y, id);

        public abstract int GetTileId(int x, int y, int layerId);
        public int GetTileId(int2 position, int layerId) => GetTileId(position.x, position.y, layerId);

        public abstract int GetOccupied(int x, int y);
        public int GetOccupied(int2 pos) => GetOccupied(pos.x, pos.y);

        public abstract bool SetTileValue(int x, int y, int value);
        public bool SetTileValue(int2 position, int value) => SetTileValue(position.x, position.y, value);

        public abstract int GetTileValue(int x, int y);
        public int GetTileValue(int2 position) => GetTileValue(position.x, position.y);

        public abstract void SetAllTiles(IEnumerable<int> tileArray);

        // Meta data is backend specific for now
        public abstract void AddDataLayerId(int x, int y, int layerId, string field, int value);
        public void AddDataLayerId(int2 pos, int layerId, string field, int value) => AddDataLayerId(pos.x, pos.y, layerId, field, value);

        public abstract void AddData(int x, int y, int layerZ, string field, int value);
        public void AddData(int3 pos, string field, int value) => AddData(pos.x, pos.y, pos.z, field, value);
        public void AddData(int2 pos, int layerZ, string field, int value) => AddData(pos.x, pos.y, layerZ, field, value);
        public void AddData(int x, int y, string field, int value) => AddData(x, y, MetaData.ColumnZ, field, value);
        public void AddData(int2 pos, string field, int value) => AddData(pos.x, pos.y, MetaData.ColumnZ, field, value);

        public abstract void AddData(int x, int y, int layerZ, ulong fieldHash, int value);
        public void AddData(int3 pos, ulong fieldHash, int value) => AddData(pos.x, pos.y, pos.z, fieldHash, value);
        public void AddData(int2 pos, int layerZ, ulong fieldHash, int value) => AddData(pos.x, pos.y, layerZ, fieldHash, value);
        public void AddData(int x, int y, ulong fieldHash, int value) => AddData(x, y, MetaData.ColumnZ, fieldHash, value);
        public void AddData(int2 pos, ulong fieldHash, int value) => AddData(pos.x, pos.y, MetaData.ColumnZ, fieldHash, value);

        public abstract int GetDataLayerId(int x, int y, int layerId, string field);
        public int GetDataLayerId(int2 pos, int layerId, string field) => GetDataLayerId(pos.x, pos.y, layerId, field);

        public abstract int GetData(int x, int y, int layerZ, string field);
        public int GetData(int3 pos, string field) => GetData(pos.x, pos.y, pos.z, field);
        public int GetData(int2 pos, int layerZ, string field) => GetData(pos.x, pos.y, layerZ, field);
        public int GetData(int x, int y, string field) => GetData(x, y, MetaData.ColumnZ, field);
        public int GetData(int2 pos, string field) => GetData(pos.x, pos.y, MetaData.ColumnZ, field);

        public abstract int GetData(int x, int y, int layerZ, ulong fieldHash);
        public int GetData(int3 pos, ulong fieldHash) => GetData(pos.x, pos.y, pos.z, fieldHash);
        public int GetData(int2 pos, int layerZ, ulong fieldHash) => GetData(pos.x, pos.y, layerZ, fieldHash);
        public int GetData(int x, int y, ulong fieldHash) => GetData(x, y, MetaData.ColumnZ, fieldHash);
        public int GetData(int2 pos, ulong fieldHash) => GetData(pos.x, pos.y, MetaData.ColumnZ, fieldHash);

        public abstract List<MetaPair> GetAllData(int3 pos);
        public abstract List<PositionValue> GetAllData(ulong fieldHash);
        public abstract List<PositionValue> GetAllData(string field);

        // Masking primitives
        public abstract void RemoveMask();
        public abstract void CreateMask(List<int> includeList, List<int> excludeList);
        public abstract void ToggleMasked(bool on);
        public abstract void AddExcludedPosition(int2 position);
        public abstract bool IsExcluding(int x, int y);
        public bool IsExcluding(int2 position) => IsExcluding(position.x, position.y);

        // Bounds primitives
        public abstract bool IsInBounds(int x, int y);
        public bool IsInBounds(int2 pos) => IsInBounds(pos.x, pos.y);

        // Region storage primitives (backend-specific container)
        protected abstract void RegionPositionsClearInternal();
        protected abstract void RegionPositionsAddInternal(int2 pos);
        protected abstract bool RegionPositionsContainsInternal(int2 pos);
        protected abstract int RegionPositionsCountInternal { get; }
        protected abstract IEnumerable<int2> RegionPositionsEnumerableInternal();

        // Grid traversal primitives
        public abstract NativeArray<int> AsNativeArray();
        public abstract IEnumerable<int2> GetPositions();
        public abstract IEnumerable<int3> GetPositions3D();
        public abstract IEnumerable<int4> GetPositionsWithId();
        public abstract IEnumerable<ITileColumn> GetColumns();
        public abstract IEnumerable<int2> GetRegionPositions();       // will call internal enumerator
        public abstract IEnumerable<ITileColumn> GetRegionColumns();  // region-based columns
        public abstract IEnumerable<ITileColumn> GetRegionGridColumns();
        public abstract IEnumerator<ITileColumn> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Lookup bundle
        public abstract void SetLookupBundle(ILookupBundle bundle);

        // Misc / numeric fields
        public abstract void ClearNumbers();
        public abstract void ClearPositiveNumbers();

        public abstract bool CanHaveTiles();

        public abstract void Dispose();

        // ============================================================
        // --------------- SHARED IMPLEMENTATIONS ---------------------
        // ============================================================

        // -- Neighbor helpers --

        private void NeighborPosHelper(int2 pos, List<int2> list)
        {
            if (IsInBounds(pos)) list.Add(pos);
        }

        public List<int2> GetEightNeighborPositions(int2 pos)
        {
            var positions = new List<int2>(8);
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
            var positions = new List<int2>(4);
            NeighborPosHelper(pos + Constants.Int2Left, positions);
            NeighborPosHelper(pos + Constants.Int2Up, positions);
            NeighborPosHelper(pos + Constants.Int2Right, positions);
            NeighborPosHelper(pos + Constants.Int2Down, positions);
            return positions;
        }

        // -- Nearest column with given tile id --

        public int2 GetNearestPosition(int x, int y, int id)
        {
            // Iterate through all distances from the center
            int maxRadius = math.max(height, width);

            for (int d = 1; d < maxRadius; d++)
            {
                for (int dx = -d; dx <= d; dx++)
                {
                    int dy1 = d - math.abs(dx);
                    int dy2 = -dy1;

                    // Top edge
                    int x1 = x + dx;
                    int y1 = y + dy1;
                    if (IsInBounds(x1, y1) && ColumnContainsId(x1, y1, id))
                        return new int2(x1, y1);

                    // Bottom edge (avoid duplicate if dy1 == dy2 == 0)
                    if (dy1 != dy2)
                    {
                        int x2 = x + dx;
                        int y2 = y + dy2;
                        if (IsInBounds(x2, y2) && ColumnContainsId(x2, y2, id))
                            return new int2(x2, y2);
                    }
                }
            }

            return Constants.OutsideGridInt2;
        }

        public int2 GetNearestPosition(int2 position, int id)
            => GetNearestPosition(position.x, position.y, id);

        // -- Random edge point --

        public int2 GetRandomEdgePoint(Random.AbstractRandom random)
        {
            int value = random.NextInt(4);

            // NOTE: x == width or y == height here is "just outside" the grid,
            // matching your existing behavior.
            return value switch
            {
                0 => new int2(width, random.NextInt(height)),
                1 => new int2(0, random.NextInt(height)),
                2 => new int2(random.NextInt(width), height),
                _ => new int2(random.NextInt(width), 0),
            };
        }

        // -- Region handling --

        public RegionBounds GetRegionBounds()
        {
            var list = new List<int2>(RegionPositionsCountInternal);

            foreach (var pos in RegionPositionsEnumerableInternal())
                list.Add(pos);

            return new RegionBounds(regionMin, regionMax, list, this);
        }

        public void SetRegionBounds(int2 min, int2 max, List<int2> regionAddPositions)
        {
            RegionPositionsClearInternal();
            regionMin = min;
            regionMax = max;
            regionLimited = true;

            if (regionAddPositions != null)
            {
                for (int i = 0; i < regionAddPositions.Count; i++)
                    RegionPositionsAddInternal(regionAddPositions[i]);
            }
        }

        public void SetRegionBounds(RegionBounds regionBounds)
        {
            SetRegionBounds(regionBounds.min, regionBounds.max, regionBounds.includingPositions);
        }

        public void RemoveRegion()
        {
            regionMin = new int2(0, 0);
            regionMax = new int2(width - 1, height - 1);
            regionLimited = false;
        }

        public void AddRegionPosition(int x, int y)
        {
            RegionPositionsAddInternal(new int2(x, y));
        }

        public void AddRegionPosition(int2 pos)
        {
            RegionPositionsAddInternal(pos);
        }

        public bool IsInRegion(int2 pos)
        {
            if (!regionLimited) return true;
            return math.all(pos >= regionMin)
                && math.all(pos <= regionMax)
                && RegionPositionsContainsInternal(pos);
        }

        public bool IsInRegion(int x, int y)
        {
            return IsInRegion(new int2(x, y));
        }

        // -- Restricted & mask helpers --

        public bool IsRestricted(int x, int y)
        {
            return !IsInBounds(x, y) || IsExcluding(x, y) || !IsInRegion(x, y);
        }

        public bool IsRestricted(int2 pos)
        {
            return IsRestricted(pos.x, pos.y);
        }

        public abstract bool IsInsideMask(int x, int y);
        public bool IsInsideMask(int2 pos) => IsInsideMask(pos.x, pos.y);

        // -- Region traversal --

        public IEnumerable<int2> GetRegionGridPositions()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
            {
                for (int x = regionMin.x; x <= regionMax.x; x++)
                {
                    yield return new int2(x, y);
                }
            }
        }
    }
}