using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Dalichrome.RandomGenerator.Core
{
    public class TileGrid : ITileGrid, IDisposable
    {
        public readonly int width;
        public readonly int height;
        public readonly int depth;

        public readonly uint seed;
        public int2 Center =>
            new int2(
                Mathf.Clamp(width / 2, 0, width),
                Mathf.Clamp(height / 2, 0, height)
            );

        public BoundsInt Bounds =>
            new BoundsInt(new Vector3Int(0, 0, 0), new Vector3Int(width, height, 1));

        private ITileGrid subgrid;
        private bool isSerial = false;

        public bool IsSerial => isSerial;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string allocationStack;
        private bool isDisposed = false;
#endif

        // ---------------- ITileGrid properties ----------------

        public int2 Minimum => subgrid.Minimum;
        public int2 Maximum => subgrid.Maximum;

        public bool IsValid
        {
            get => subgrid.IsValid;
            set => subgrid.IsValid = value;
        }

        public bool Masked => subgrid.Masked;

        public ITileMask TileMask
        {
            get => subgrid.TileMask;
            set => subgrid.TileMask = value;
        }

        public bool IsIncludingTiles
        {
            get
            {
                if (!subgrid.TileMask.IsValid) return false;
                return subgrid.TileMask.IsIncludingTiles;
            }
        }

        public bool IsExcludingTiles
        {
            get
            {
                if (!subgrid.TileMask.IsValid) return false;
                return subgrid.TileMask.IsExcludingTiles;
            }
        }

        // ---------------- ctor / cloning ----------------

        public TileGrid(int width, int height, int depth, uint seed)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;

            subgrid = new NativeTileGrid(width, height, depth, seed);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            allocationStack = Environment.StackTrace;
#endif
        }

        public static TileGrid DeepClone(TileGrid other)
        {
            var grid = new TileGrid(other.width, other.height, other.depth, other.seed);
            if (other.IsValid)
                grid.subgrid = other.subgrid.DeepClone();
            return grid;
        }

        // For internal wiring when NativeTileGrid (struct) changes
        private void SetGridData(NativeTileGrid newData) => subgrid = newData;

        // Exposed Helpers

        public int GetLayerIndexFromTileId(int id) =>
            subgrid.GetLayerIndexFromTileId(id);
        public int GetLayerIndexFromLayerId(int id) =>
            subgrid.GetLayerIndexFromTileId(id);

        // ---------------- Primitive tile / column ops ----------------

        public bool SetTileId(int x, int y, int id) =>
            subgrid.SetTileId(x, y, id);

        // sugar
        public bool SetTileId(int2 position, int id) =>
            SetTileId(position.x, position.y, id);

        public bool SetTileId(Vector2Int position, int id) =>
            SetTileId(position.x, position.y, id);

        public bool SetTileIdBypassLayer(int x, int y, int z, int value) =>
            subgrid.SetTileIdBypassLayer(x, y, z, value);

        // sugar
        public bool SetTileIdBypassLayer(int3 position, int value) =>
            SetTileIdBypassLayer(position.x, position.y, position.z, value);

        public bool SetTileIdBypassLayer(int2 position, int z, int value) =>
            SetTileIdBypassLayer(position.x, position.y, z, value);

        public bool CopyColumn(int2 replacer, int2 replaced) =>
            subgrid.CopyColumn(replacer, replaced);

        public ITileColumn GetColumn(int x, int y) =>
            subgrid.GetColumn(x, y);

        // sugar
        public ITileColumn GetColumn(int2 pos) =>
            GetColumn(pos.x, pos.y);

        public ITileColumn GetColumn(Vector2Int pos) =>
            GetColumn(pos.x, pos.y);

        public bool SetColumn(ITileColumn col) =>
            subgrid.SetColumn(col);

        public int GetEmpty(int id) =>
            subgrid.GetEmpty(id);

        // sugar
        public bool GetEmptyBool(int id) =>
            subgrid.GetEmpty(id) == 1;

        public int GetNotEmpty(int id) =>
            -(subgrid.GetEmpty(id) - 1);

        public bool ColumnContainsId(int x, int y, int id) =>
            subgrid.ColumnContainsId(x, y, id);

        // sugar
        public bool ColumnContainsId(int2 position, int id) =>
            ColumnContainsId(position.x, position.y, id);

        public bool ColumnContainsId(Vector2Int position, int id) =>
            ColumnContainsId(position.x, position.y, id);

        public int GetTileId(int x, int y, int layerId) =>
            subgrid.GetTileId(x, y, layerId);

        // sugar
        public int GetTileId(int2 position, int layerId) =>
            GetTileId(position.x, position.y, layerId);

        public int GetTileId(Vector2Int position, int layerId) =>
            GetTileId(position.x, position.y, layerId);

        public int GetTileIdWithLayerIndex(int x, int y, int z)
        {
            return subgrid.GetTileIdWithLayerIndex(x, y, z);
        }

        public int GetTileIdWithLayerIndex(int3 pos)
        {
            return GetTileIdWithLayerIndex(pos.x, pos.y, pos.z);
        }

        public int GetOccupied(int x, int y) =>
            subgrid.GetOccupied(x, y);

        // sugar
        public int GetOccupied(int2 pos) =>
            GetOccupied(pos.x, pos.y);

        public bool SetTileValue(int x, int y, int value) =>
            subgrid.SetTileValue(x, y, value);

        // sugar
        public bool SetTileValue(int2 position, int value) =>
            SetTileValue(position.x, position.y, value);

        public bool SetTileValue(Vector2Int position, int value) =>
            SetTileValue(position.x, position.y, value);

        public int GetTileValue(int x, int y) =>
            subgrid.GetTileValue(x, y);

        // sugar
        public int GetTileValue(int2 position) =>
            GetTileValue(position.x, position.y);

        public int GetTileValue(Vector2Int position) =>
            GetTileValue(position.x, position.y);

        public void SetAllTiles(IEnumerable<int> tileArray) =>
            subgrid.SetAllTiles(tileArray);

        // ---------------- Meta data primitives + sugar ----------------

        public void AddDataLayerId(int x, int y, int layerId, string field, int value) =>
            subgrid.AddDataLayerId(x, y, layerId, field, value);

        public void AddDataLayerId(int2 pos, int layerId, string field, int value) =>
            AddDataLayerId(pos.x, pos.y, layerId, field, value);

        public void AddData(int x, int y, int layerZ, string field, int value) =>
            subgrid.AddData(x, y, layerZ, field, value);

        public void AddData(int3 pos, FixedString64Bytes field, int value) =>
       subgrid.AddData(pos.x, pos.y, pos.z, field, value);

        public void AddData(int3 pos, string field, int value) =>
            subgrid.AddData(pos.x, pos.y, pos.z, field, value);

        public void AddData(MetadataEntry entry) =>
            subgrid.AddData(entry);

        // sugar
        public void AddData(int2 pos, int layerZ, string field, int value) =>
            AddData(pos.x, pos.y, layerZ, field, value);

        public void AddData(int x, int y, string field, int value) =>
            AddData(x, y, MetaData.ColumnZ, field, value);

        public void AddData(int2 pos, string field, int value) =>
            AddData(pos.x, pos.y, MetaData.ColumnZ, field, value);

        public void AddData(int x, int y, int layerZ, FixedString64Bytes fixedField, int value) =>
            subgrid.AddData(x, y, layerZ, fixedField, value);

        // sugar
        public void AddData(int2 pos, int layerZ, FixedString64Bytes fixedField, int value) =>
            AddData(pos.x, pos.y, layerZ, fixedField, value);

        public void AddData(int x, int y, FixedString64Bytes fixedField, int value) =>
            AddData(x, y, MetaData.ColumnZ, fixedField, value);

        public void AddData(int2 pos, FixedString64Bytes fixedField, int value) =>
            AddData(pos.x, pos.y, MetaData.ColumnZ, fixedField, value);

        public int GetDataLayerId(int x, int y, int layerId, string field) =>
            subgrid.GetDataLayerId(x, y, layerId, field);

        public int GetDataLayerId(int2 pos, int layerId, string field) =>
            GetDataLayerId(pos.x, pos.y, layerId, field);

        public int GetData(int x, int y, int layerZ, string field) =>
            subgrid.GetData(x, y, layerZ, field);

        // sugar
        public int GetData(int3 pos, string field) =>
            GetData(pos.x, pos.y, pos.z, field);

        public int GetData(int2 pos, int layerZ, string field) =>
            GetData(pos.x, pos.y, layerZ, field);

        public int GetData(int x, int y, string field) =>
            GetData(x, y, MetaData.ColumnZ, field);

        public int GetData(int2 pos, string field) =>
            GetData(pos.x, pos.y, MetaData.ColumnZ, field);

        public int GetData(int x, int y, int layerZ, FixedString64Bytes fixedField) =>
            subgrid.GetData(x, y, layerZ, fixedField);

        public int GetData(int3 pos, FixedString64Bytes fixedField) =>
            GetData(pos.x, pos.y, pos.z, fixedField);

        public int GetData(int2 pos, int layerZ, FixedString64Bytes fixedField) =>
            GetData(pos.x, pos.y, layerZ, fixedField);

        public int GetData(int x, int y, FixedString64Bytes fixedField) =>
            GetData(x, y, MetaData.ColumnZ, fixedField);

        public int GetData(int2 pos, FixedString64Bytes fixedField) =>
            GetData(pos.x, pos.y, MetaData.ColumnZ, fixedField);

        public List<MetaPair> GetAllData(int3 pos) =>
            subgrid.GetAllData(pos);

        public List<MetaPair> GetAllData(int2 pos) =>
            subgrid.GetAllData(new int3(pos.x, pos.y, MetaData.ColumnZ));

        public List<MetaPair> GetAllDataWithColData(int3 pos) =>
            subgrid.GetAllDataWithColData(pos);

        public List<MetaPair> GetAllDataWithColData(int2 pos) =>
            subgrid.GetAllDataWithColData(new int3(pos.x, pos.y, MetaData.ColumnZ));

        public List<PositionValue> GetAllData(FixedString64Bytes fixedField) =>
            subgrid.GetAllData(fixedField);

        public List<PositionValue> GetAllData(string field) =>
            subgrid.GetAllData(field);

        public int RemoveAllAt(int3 pos) => subgrid.RemoveAllAt(pos);

        public int RemoveAllAt(int2 pos) => subgrid.RemoveAllAt(new int3(pos.x, pos.y, MetaData.ColumnZ));

        public List<string> GetMetaFields() => subgrid.GetMetaFields();

        // ---------------- Masking ----------------

        public void RemoveMask() =>
            subgrid.RemoveMask();

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
            subgrid.CreateMask(includeList, excludeList);

            // If NativeTileGrid is a struct, this keeps our local copy in sync
            if (subgrid is NativeTileGrid grid)
                SetGridData(grid);
        }

        public void ToggleMasked(bool on) => subgrid.ToggleMasked(on);

        public void AddExcludedPosition(int2 position) => subgrid.AddExcludedPosition(position);
        public void AddExcludedPosition(int x, int y) => AddExcludedPosition(new int2( x, y));
        public void AddExcludedPosition(Vector2Int position) => AddExcludedPosition(position.x, position.y);

        public bool IsExcluding(int x, int y) => IsExcluding(new int2(x, y));
        public bool IsExcluding(int2 position) => subgrid.IsExcluding(position);
        public bool IsExcluding(Vector2Int position) => IsExcluding(position.x, position.y);

        public bool IsInsideMask(int x, int y) => subgrid.IsInsideMask(x, y);
        public bool IsInsideMask(int2 pos) => IsInsideMask(pos.x, pos.y);

        // ---------------- Neighbor helpers (shared logic) ----------------

        private void NeighborPosHelper(int2 pos, List<int2> list)
        {
            if (IsInBounds(pos.x, pos.y))
                list.Add(pos);
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

        // ---------------- Nearest / random edge (shared logic) ----------------

        public int2 GetNearestPosition(int x, int y, int id)
        {
            int maxRadius = math.max(height, width);

            for (int d = 1; d < maxRadius; d++)
            {
                for (int dx = -d; dx <= d; dx++)
                {
                    int dy1 = d - math.abs(dx);
                    int dy2 = -dy1;

                    int x1 = x + dx;
                    int y1 = y + dy1;
                    if (IsInBounds(x1, y1) && ColumnContainsId(x1, y1, id))
                        return new int2(x1, y1);

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

        // sugar
        public int2 GetNearestPosition(int2 position, int id) =>
            GetNearestPosition(position.x, position.y, id);

        public Vector2Int GetNearestPosition(Vector2Int position, int id)
        {
            int2 nearest = GetNearestPosition(position.x, position.y, id);
            return new Vector2Int(nearest.x, nearest.y);
        }

        public int2 GetRandomEdgePoint(AbstractRandom random)
        {
            int value = random.NextInt(4);

            // x == width or y == height => just outside grid, preserving prior behavior
            return value switch
            {
                0 => new int2(width, random.NextInt(height)),
                1 => new int2(0, random.NextInt(height)),
                2 => new int2(random.NextInt(width), height),
                _ => new int2(random.NextInt(width), 0),
            };
        }

        public Vector2Int GetRandomEdgeVector2(AbstractRandom random)
        {
            int2 p = GetRandomEdgePoint(random);
            return new Vector2Int(p.x, p.y);
        }

        // ---------------- Region API (delegated) ----------------

        public RegionBounds GetRegionBounds() =>
            subgrid.GetRegionBounds();

        public void SetRegionBounds(int2 min, int2 max, List<int2> regionExcludedPositions) =>
            subgrid.SetRegionBounds(min, max, regionExcludedPositions);

        public void SetRegionBounds(Vector2Int min, Vector2Int max, List<int2> regionExcludedPositions) =>
            SetRegionBounds(new int2(min.x, min.y), new int2(max.x, max.y), regionExcludedPositions);

        public void SetRegionBounds(RegionBounds regionBounds) =>
            SetRegionBounds(regionBounds.min, regionBounds.max, regionBounds.includingPositions);

        public void RemoveRegion() =>
            subgrid.RemoveRegion();

        public void AddRegionPosition(int x, int y) => subgrid.AddRegionPosition(x, y);
        public void AddRegionPosition(int2 pos) => AddRegionPosition(pos.x, pos.y);
        public void AddRegionPosition(Vector2Int pos) => AddRegionPosition(pos.x, pos.y);

        public bool IsInRegion(int x, int y) => subgrid.IsInRegion(x, y);
        public bool IsInRegion(int2 pos) => IsInRegion(pos.x, pos.y);
        public bool IsInRegion(Vector2Int pos) => IsInRegion(pos.x, pos.y);

        // ---------------- Bounds / restriction (shared logic) ----------------

        public bool IsInBounds(int x, int y) => subgrid.IsInBounds(x, y);
        public bool IsInBounds(int2 pos) => IsInBounds(pos.x, pos.y);
        public bool IsInBounds(Vector2Int pos) => IsInBounds(pos.x, pos.y);

        // Restricted = out of bounds OR excluded OR out of region
        public bool IsRestricted(int x, int y) => !IsInBounds(x, y) || IsExcluding(x, y) || !IsInRegion(x, y);
        public bool IsRestricted(int2 pos) => IsRestricted(pos.x, pos.y);
        public bool IsRestricted(Vector2Int pos) => IsRestricted(pos.x, pos.y);

        // ---------------- Misc ----------------

        public void ClearNumbers() =>
            subgrid.ClearNumbers();

        public void ClearPositiveNumbers() =>
            subgrid.ClearPositiveNumbers();

        public bool CanHaveTiles() =>
            subgrid.CanHaveTiles();

        public void SetLookupBundle(ILookupBundle bundle)
        {
            if ((!isSerial && bundle is NativeLookupBundle) ||
                (isSerial && bundle is SerialLookupBundle))
            {
                subgrid.SetLookupBundle(bundle);
            }
        }

        public ILookupBundle GetLookupBundle()
        {
            return subgrid.GetLookupBundle();
        }

        public NativeTileGrid GetNative() =>
            subgrid is NativeTileGrid grid ? grid : default;

        public NativeTileGrid CloneNativeGrid()
        {
            if (subgrid is NativeTileGrid grid)
                return NativeTileGrid.DeepClone(grid);
            return default;
        }

        public void OverrideSubGrid(NativeTileGrid data)
        {
            subgrid.Dispose();
            subgrid = data;
        }

        public void ToSerial()
        {
            if (subgrid is NativeTileGrid nativeGrid)
            {
                SerialTileGrid newGrid = nativeGrid.ToSerial();
                nativeGrid.Dispose();
                subgrid = newGrid;
                isSerial = true;
            }
        }

        // ---------------- Enumeration ----------------

        public NativeArray<int> AsNativeArray() =>
            subgrid.AsNativeArray();

        public IEnumerable<int2> GetPositions() =>
            subgrid.GetPositions();

        public IEnumerable<int3> GetPositions3D() =>
            subgrid.GetPositions3D();

        public IEnumerable<int4> GetPositionsWithId() =>
            subgrid.GetPositionsWithId();

        public IEnumerable<ITileColumn> GetColumns() =>
            subgrid.GetColumns();

        public IEnumerable<int2> GetRegionPositions() =>
            subgrid.GetRegionPositions();

        public IEnumerable<ITileColumn> GetRegionColumns() =>
            subgrid.GetRegionColumns();

        public IEnumerable<int2> GetRegionGridPositions() =>
            subgrid.GetRegionGridPositions();

        public IEnumerable<ITileColumn> GetRegionGridColumns() =>
            subgrid.GetRegionGridColumns();

        public IEnumerator<ITileColumn> GetEnumerator() =>
            subgrid.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        // ---------------- Lifetime ----------------

        public void Dispose()
        {
            subgrid.Dispose();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            isDisposed = true;
#endif
        }

        ~TileGrid()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!isDisposed && subgrid is NativeTileGrid)
            {
                Debug.LogError(
                    $"[TileGrid] Native memory leak detected! TileGrid was not disposed properly.\n" +
                    $"Allocation stack:\n{allocationStack}"
                );
            }
#endif
        }
    }
}
