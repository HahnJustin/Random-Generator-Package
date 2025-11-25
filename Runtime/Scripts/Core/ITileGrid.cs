using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{

    public interface ITileGrid : IDisposable, IEnumerable<ITileColumn>
    {
        public int2 Minimum { get; }
        public int2 Maximum { get; }

        // Validity
        public bool IsValid { get; set; }

        // Masking Properties
        public bool IsIncludingTiles { get; }
        public bool IsExcludingTiles { get; }

        // Set Tile Id Funcs
        public abstract bool SetTileId(int x, int y, int id);
        public abstract bool SetTileId(int2 position, int id);

        // Set Tile Id Bypass Layer Funcs
        public abstract bool SetTileIdBypassLayer(int3 position, int value);
        public abstract bool SetTileIdBypassLayer(int x, int y, int z, int value);

        // Set All Ids At Pos Funcs
        public abstract bool CopyColumn(int2 replacer, int2 replaced);

        // Get Column
        public abstract ITileColumn GetColumn(int2 pos);
        public abstract ITileColumn GetColumn(int x, int y);

        // Set Column
        public abstract bool SetColumn(ITileColumn col);

        // Get Empty
        public abstract int GetEmpty(int id);

        // Contains Id Funcs
        public abstract bool ColumnContainsId(int x, int y, int id);
        public abstract bool ColumnContainsId(int2 position, int id);

        // Get Tile Id Funcs
        public abstract int GetTileId(int x, int y, int layerId);
        public abstract int GetTileId(int2 position, int layerId);

        // Get Occupied
        public abstract int GetOccupied(int2 pos);
        public abstract int GetOccupied(int x, int y);

        // Set Tile Value Funcs
        public abstract bool SetTileValue(int x, int y, int value);
        public abstract bool SetTileValue(int2 position, int value);

        // Get Tile Value Funcs
        public abstract int GetTileValue(int x, int y);
        public abstract int GetTileValue(int2 position);

        // Set All Tiles
        public abstract void SetAllTiles(IEnumerable<int> tileArray);

        // Meta Data Func
        public void AddDataLayerId(int2 pos, int layerId, string field, int value);
        public void AddDataLayerId(int x, int y, int layerId, string field, int value);

        public void AddData(int x, int y, int layerZ, string field, int value);
        public void AddData(int2 pos, int layerZ, string field, int value);
        public void AddData(int x, int y, string field, int value);

        public void AddData(int x, int y, int layerZ, ulong fieldHash, int value);
        public void AddData(int2 pos, int layerZ, ulong fieldHash, int value);
        public void AddData(int x, int y, ulong fieldHash, int value);

        public int GetDataLayerId(int2 pos, int layerId, string field);
        public int GetDataLayerId(int x, int y, int layerId, string field);

        public int GetData(int x, int y, int layerZ, string field);
        public int GetData(int2 pos, int layerZ, string field);
        public int GetData(int x, int y, string field);
        public int GetData(int2 pos, string field);

        public int GetData(int x, int y, int layerZ, ulong fieldHash);
        public int GetData(int2 pos, int layerZ, ulong fieldHash);
        public int GetData(int x, int y, ulong fieldHash);
        public int GetData(int2 pos, ulong fieldHash);

        public List<MetaPair> GetAllData(int3 pos);
        public List<PositionValue> GetAllData(ulong fieldHash);
        public List<PositionValue> GetAllData(string field);

        // Masking Funcs
        public bool Masked { get; }
        public ITileMask TileMask { get; set; }
        public abstract void RemoveMask();
        public abstract void CreateMask(List<int> includeList, List<int> excludeList);
        public abstract void ToggleMasked(bool on);
        public abstract void AddExcludedPosition(int2 position);
        public abstract bool IsExcluding(int2 position);
        public abstract bool IsExcluding(int x, int y);

        // Helper Funcs - could move
        public abstract int2 GetNearestPosition(int x, int y, int id);
        public abstract int2 GetNearestPosition(int2 position, int id);
        public abstract int2 GetRandomEdgePoint(AbstractRandom random);
        public abstract void ClearNumbers();
        public abstract void ClearPositiveNumbers();

        // Validity Func
        public abstract bool CanHaveTiles();

        // Transversal Funcs
        public abstract List<int2> GetEightNeighborPositions(int2 pos);
        public abstract List<int2> GetFourNeighborPositions(int2 pos);

        // Region Funcs
        public abstract RegionBounds GetRegionBounds();
        public abstract void SetRegionBounds(int2 min, int2 max, List<int2> regionExcludedPositions);
        public abstract void SetRegionBounds(RegionBounds regionBounds);
        public abstract void RemoveRegion();

        public abstract void AddRegionPosition(int x, int y);
        public abstract void AddRegionPosition(int2 pos);

        public abstract bool IsInRegion(int x, int y);
        public abstract bool IsInRegion(int2 pos);

        // Bounds Checking Funcs
        public abstract bool IsInBounds(int x, int y);
        public abstract bool IsInBounds(int2 pos);

        // Restricted = Either Universal Mask Excluded, Out of Bounds or Region Excluded
        public abstract bool IsRestricted(int x, int y);
        public abstract bool IsRestricted(int2 pos);

        // IsInsideMask
        public abstract bool IsInsideMask(int x, int y);

        public abstract bool IsInsideMask(int2 pos);

        // Ienumeration
        public abstract NativeArray<int> AsNativeArray();
        public abstract IEnumerable<int2> GetPositions();
        public abstract IEnumerable<int3> GetPositions3D();
        public abstract IEnumerable<int4> GetPositionsWithId();
        public abstract IEnumerable<ITileColumn> GetColumns();
        public abstract IEnumerable<int2> GetRegionPositions();
        public abstract IEnumerable<ITileColumn> GetRegionColumns();
        public abstract IEnumerable<int2> GetRegionGridPositions();
        public abstract IEnumerable<ITileColumn> GetRegionGridColumns();


        // Layer Lookup Funcs
        public abstract void SetLookupBundle(ILookupBundle bundle);

    }
}