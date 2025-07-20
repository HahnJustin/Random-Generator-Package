using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Dalichrome.RandomGenerator.Random;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public class TileGrid : ITileGrid, IDisposable, IEnumerable
    {
        public readonly int width;
        public readonly int height;

        public Vector2Int Center { get { return new Vector2Int(Mathf.Clamp(width / 2, 0, width), Mathf.Clamp(height / 2, 0, height)); } }

        public BoundsInt Bounds { get { return new(new Vector3Int(0, 0, 0), new Vector3Int(width, height, 1)); } }

        public bool Masked { get { return subgrid.Masked; } }

        private NativeTileGrid subgrid;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string allocationStack;
        private bool isDisposed = false;
#endif

        public bool IsValid 
        {
            get { return subgrid.IsValid; }
            internal set { subgrid.IsValid = value; }
        }

        public bool IsIncludingTiles
        {
            get
            {
                if (!subgrid.tileMask.IsValid) return false;
                return subgrid.tileMask.IsIncludingTiles;
            }
        }
        public bool IsExcludingTiles
        {
            get
            {
                if (!subgrid.tileMask.IsValid) return false;
                return subgrid.tileMask.IsExcludingTiles;
            }
        }

        public int2 Minimum
        {
            get { return subgrid.Minimum; }
        }

        public int2 Maximum
        {
            get { return subgrid.Maximum; }
        }

        public TileGrid(int width, int height)
        {
            this.width = width;
            this.height = height;

            subgrid = new (width, height);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            allocationStack = Environment.StackTrace;
#endif
        }

        public static TileGrid DeepClone(TileGrid other)
        {
            TileGrid grid = new(other.width, other.height);
            if(other.IsValid) grid.subgrid = other.subgrid.DeepClone();
            return grid;
        }

        // Private Funcs
        public void SetGridData(NativeTileGrid newData) => subgrid = newData;

        // Set Tile Type
        public bool SetTileId(int x, int y, int id)
        {
            return subgrid.SetTileId(x, y, id);
        }

        public bool SetTileId(Vector2Int position, int id)
        {
            return SetTileId(position.x, position.y, id);
        }

        public bool SetTileId(int2 position, int id)
        {
            return SetTileId(position.x, position.y, id);
        }

        public bool SetTileId(Tile tile, int id)
        {
            return SetTileId(tile.x, tile.y, id);
        }


        // Contains Type
        public bool ContainsId(int x, int y, int id)
        {
            return subgrid.ContainsId(x, y, id);
        }

        public bool ContainsId(int2 position, int id)
        {
            return ContainsId(position.x, position.y, id);
        }

        public bool ContainsId(Vector2Int position, int id)
        {
            return ContainsId(position.x, position.y, id);
        }

        // Get Tile
        public Tile GetTile(int x, int y)
        {
            return subgrid.GetTile(x, y);
        }

        public Tile GetTile(Vector2Int position)
        {
            return GetTile(position.x, position.y);
        }

        public Tile GetTile(int2 position)
        {
            return GetTile(position.x, position.y);
        }


        // Set Tile
        public bool SetTile(int x, int y, Tile toSet)
        {
            subgrid.SetTile(x, y, toSet);
            return true;
        }

        public bool SetTile(int2 position, Tile toSet)
        {
            return SetTile(position.x, position.y, toSet);
        }

        public bool SetTile(Vector2Int position, Tile toSet)
        {
            return SetTile(position.x, position.y, toSet);
        }

        public bool SetTile(Tile oldTile, Tile toSet)
        {
            return SetTile(oldTile.Position, toSet);
        }

        // Set Tile Value
        public bool SetTileValue(int x, int y, int value)
        {
            return subgrid.SetTileValue(x, y, value);
        }

        public bool SetTileValue(Vector2Int position, int value)
        {
            return SetTileValue(position.x, position.y, value);
        }

        public bool SetTileValue(int2 position, int value)
        {
            return SetTileValue(position.x, position.y, value);
        }

        public bool SetTileValue(Tile tile, int value)
        {
            tile.SetValue(value);
            return SetTileValue(tile.x, tile.y, value);
        }

        // Mask Funcs
        public void RemoveMask()
        {
            subgrid.RemoveMask();
        }

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
            subgrid.CreateMask(includeList, excludeList);
            SetGridData(subgrid);
        }

        public void ToggleMasked(bool on)
        {
            subgrid.ToggleMasked(on);
        }

        public void AddExcludedPosition(int2 position)
        {
            subgrid.AddExcludedPosition(position);
        }

        public void AddExcludedPosition(Vector2Int position)
        {
            AddExcludedPosition(new int2(position.x, position.y));
        }

        public bool IsExcluding(int x, int y)
        {
            return IsExcluding(new int2(x, y));
        }

        public bool IsExcluding(Vector2Int position)
        {
            return IsExcluding(position.x, position.y);
        }

        public bool IsExcluding(int2 position)
        {
            return subgrid.IsExcluding(position);
        }

        public bool IsExcluding(Tile tile)
        {
            return IsExcluding(tile.Int2);
        }

        public int2 GetNearestPosition(int x, int y, int id)
        {
            return subgrid.GetNearestPosition(x, y, id);
        }

        public Vector2Int GetNearestPosition(Vector2Int position, int id)
        {
            int2 nearest =  GetNearestPosition(position.x, position.y, id);
            return new Vector2Int(nearest.x, nearest.y);
        }

        public int2 GetNearestPosition(int2 position, int id)
        {
            return GetNearestPosition(position.x, position.y, id);
        }

        public int2 GetRandomEdgePoint(AbstractRandom random)
        {
            return subgrid.GetRandomEdgePoint(random);
        }

        public Vector2Int GetRandomEdgeVector2(AbstractRandom random)
        {
            int2 position = subgrid.GetRandomEdgePoint(random);
            return new(position.x, position.y);
        }

        public void ClearNumbers()
        {
            subgrid.ClearNumbers();
        }

        public void ClearPositiveNumbers()
        {
           subgrid.ClearPositiveNumbers();
        }

        public bool CanHaveTiles()
        {
            return subgrid.CanHaveTiles();
        }

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
            if (!isDisposed)
            {
                Debug.LogError($"[TileGrid] Native memory leak detected! TileGrid was not disposed properly.\nAllocation stack:\n{allocationStack}");
            }
#endif
        }


        public List<Tile> GetEightNeighborTiles(Tile tile)
        {
            return subgrid.GetEightNeighborTiles(tile);
        }

        public List<Tile> GetFourNeighborTiles(Tile tile)
        {
            return subgrid.GetFourNeighborTiles(tile);
        }


        public bool IsInBounds(int x, int y)
        {
            return subgrid.IsInBounds(x, y);
        }

        public bool IsInBounds(int2 position)
        {
            return subgrid.IsInBounds(position);
        }

        public RegionBounds GetRegionBounds()
        {
            return subgrid.GetRegionBounds();
        }

        public void SetRegionBounds(RegionBounds regionBounds)
        {
            subgrid.SetRegionBounds(regionBounds);
        }

        public void SetRegionBounds(int2 min, int2 max, List<int2> regionExcludedPositions) 
        {
            subgrid.SetRegionBounds(min, max, regionExcludedPositions);
        }

        public void SetRegionBounds(Vector2Int min, Vector2Int max, List<int2> regionExcludedPositions)
        {
            subgrid.SetRegionBounds(new (min.x, min.y), new(max.x, max.y), regionExcludedPositions);
        }

        public void RemoveRegion()
        {
            subgrid.RemoveRegion();
        }

        public void AddRegionPosition(int x, int y)
        {
            subgrid.AddRegionPosition(x, y);
        }

        public void AddRegionPosition(Vector2Int pos)
        {
            subgrid.AddRegionPosition(pos.x, pos.y);
        }

        public void AddRegionPosition(int2 pos)
        {
            subgrid.AddRegionPosition(pos);
        }

        public bool IsInRegion(Tile tile)
        {
            return subgrid.IsInRegion(tile);
        }

        public bool IsInRegion(Vector2Int pos)
        {
            return subgrid.IsInRegion(pos.x, pos.y);
        }

        public bool IsInRegion(int2 pos) 
        {
            return subgrid.IsInRegion(pos);
        }

        public bool IsInRegion(int x, int y)
        {
            return subgrid.IsInRegion(x, y);
        }

        public bool IsRestricted(int x, int y)
        {
            return subgrid.IsRestricted(x, y);
        }

        public bool IsRestricted(Vector2Int pos)
        {
            return subgrid.IsRestricted(pos.x, pos.y);
        }

        public bool IsRestricted(int2 pos)
        {
            return subgrid.IsRestricted(pos);
        }

        // Ienumeration
        public IEnumerator GetEnumerator()
        {
            return subgrid.GetEnumerator();
        }

        public NativeArray<Tile> AsNativeArray()
        {
            return subgrid.AsNativeArray();
        }

        public IEnumerable<Tile> GetRegionTiles()
        {
            return subgrid.GetRegionTiles();
        }

        public IEnumerable<Tile> GetRegionGrid()
        {
            return subgrid.GetRegionGrid();
        }

        public ref NativeTileGrid GetNative() => ref subgrid;

        public NativeTileGrid CloneNativeGrid()
        {
            return NativeTileGrid.DeepClone(subgrid);
        }

        public void OverrideSubGrid(NativeTileGrid _data)
        {
            subgrid.Dispose();
            subgrid = _data;
        }

        public void AddLayersLookups(Dictionary<int, LayerType> layerLookup)
        {
            subgrid.AddLayersLookups(layerLookup);
        }
    }
}