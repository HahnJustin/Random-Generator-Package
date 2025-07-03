using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{

    public interface ITileGrid
    {

        // Validity
        public bool IsValid { get; }

        // Masking Properties
        public bool IsIncludingTiles { get; }
        public bool IsExcludingTiles { get; }

        // Set Tile Id Funcs
        public abstract bool SetTileId(int x, int y, int id);
        public abstract bool SetTileId(int2 position, int id);
        public abstract bool SetTileId(Tile tile, int id);

        // Contains Id Funcs
        public abstract bool ContainsId(int x, int y, int id);
        public abstract bool ContainsId(int2 position, int id);

        // Get Tile Funcs
        public abstract Tile GetTile(int x, int y);
        public abstract Tile GetTile(int2 position);

        // Set Tile Funcs
        public abstract bool SetTile(int x, int y, Tile toSet);
        public abstract bool SetTile(int2 position, Tile toSet);
        public abstract bool SetTile(Tile oldTile, Tile toSet);

        // Set Tile Value Funcs
        public abstract bool SetTileValue(int x, int y, int value);
        public abstract bool SetTileValue(int2 position, int value);
        public abstract bool SetTileValue(Tile tile, int value);

        // Masking Funcs
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
        public abstract List<Tile> GetEightNeighborTiles(Tile tile);
        public abstract List<Tile> GetFourNeighborTiles(Tile tile);

        // Region Funcs
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

        // Restricted = Either Mask Excluded, Out of Bounds or Region Excluded
        public abstract bool IsRestricted(int x, int y);
        public abstract bool IsRestricted(int2 pos);

        // Ienumeration
        public abstract NativeArray<Tile> AsNativeArray();
        public abstract IEnumerable<Tile> GetRegionPositions();
        public abstract IEnumerable<Tile> GetRegionGrid();

        // Layer Table Funcs
        public abstract void AddLayersLookups(Dictionary<int, LayerType> layerLookup);
    }
}