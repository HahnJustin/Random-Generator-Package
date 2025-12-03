using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public interface ITileGrid : IDisposable, IEnumerable<ITileColumn>
    {
        int2 Minimum { get; }
        int2 Maximum { get; }

        // Validity
        bool IsValid { get; set; }

        // Masking Properties
        bool Masked { get; }
        ITileMask TileMask { get; set; }
        bool IsIncludingTiles { get; }
        bool IsExcludingTiles { get; }

        // ---------------- Tile / column primitives ----------------

        bool SetTileId(int x, int y, int id);
        bool SetTileIdBypassLayer(int x, int y, int z, int value);

        bool CopyColumn(int2 replacer, int2 replaced);

        ITileColumn GetColumn(int x, int y);
        bool SetColumn(ITileColumn col);

        int GetEmpty(int id);
        bool ColumnContainsId(int x, int y, int id);

        int GetTileId(int x, int y, int layerId);
        int GetOccupied(int x, int y);

        bool SetTileValue(int x, int y, int value);
        int GetTileValue(int x, int y);

        void SetAllTiles(IEnumerable<int> tileArray);

        // ---------------- Meta data primitives ----------------

        void AddDataLayerId(int x, int y, int layerId, string field, int value);

        void AddData(int x, int y, int layerZ, string field, int value);
        void AddData(int x, int y, int layerZ, FixedString64Bytes fixedField, int value);

        int GetDataLayerId(int x, int y, int layerId, string field);

        int GetData(int x, int y, int layerZ, string field);
        int GetData(int x, int y, int layerZ, FixedString64Bytes fixedField);

        List<MetaPair> GetAllData(int3 pos);
        List<PositionValue> GetAllData(FixedString64Bytes fixedField);
        List<PositionValue> GetAllData(string field);

        // ---------------- Masking primitives ----------------

        void RemoveMask();
        void CreateMask(List<int> includeList, List<int> excludeList);
        void ToggleMasked(bool on);

        void AddExcludedPosition(int2 pos);
        bool IsExcluding(int2 pos);
        bool IsInsideMask(int x, int y);

        // ---------------- Helpers / misc ----------------

        void ClearNumbers();
        void ClearPositiveNumbers();

        bool CanHaveTiles();

        // ---------------- Region funcs ----------------

        RegionBounds GetRegionBounds();
        void SetRegionBounds(int2 min, int2 max, List<int2> regionExcludedPositions);
        void RemoveRegion();

        void AddRegionPosition(int x, int y);
        bool IsInRegion(int x, int y);

        // ---------------- Bounds / restriction ----------------

        bool IsInBounds(int x, int y);
        bool IsRestricted(int x, int y);

        // ---------------- Iteration ----------------

        NativeArray<int> AsNativeArray();
        IEnumerable<int2> GetPositions();
        IEnumerable<int3> GetPositions3D();
        IEnumerable<int4> GetPositionsWithId();
        IEnumerable<ITileColumn> GetColumns();
        IEnumerable<int2> GetRegionPositions();
        IEnumerable<ITileColumn> GetRegionColumns();
        IEnumerable<int2> GetRegionGridPositions();
        IEnumerable<ITileColumn> GetRegionGridColumns();

        // ---------------- Layer lookup ----------------

        void SetLookupBundle(ILookupBundle bundle);
    }
}
