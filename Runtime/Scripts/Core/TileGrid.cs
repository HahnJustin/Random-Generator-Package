using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Dalichrome.RandomGenerator.Random;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Collections.AllocatorManager;

namespace Dalichrome.RandomGenerator.Core
{
    public class TileGrid : IDisposable, IEnumerable
    {
        public readonly int width;
        public readonly int height;

        public Vector2Int Center { get { return new Vector2Int(Mathf.Clamp(width / 2, 0, width), Mathf.Clamp(height / 2, 0, height)); } }

        public BoundsInt Bounds { get { return new(new Vector3Int(0, 0, 0), new Vector3Int(width, height, 1)); } }

        public bool Masked { get { return data.Masked; } }

        private NativeTileGrid data;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string allocationStack;
        private bool isDisposed = false;
#endif

        public bool IsDataValid 
        {
            get { return data.IsValid; }
        }

        public bool IsIncludingTiles
        {
            get
            {
                if (!data.tileMask.IsValid) return false;
                return data.tileMask.IsIncludingTiles;
            }
        }
        public bool IsExcludingTiles
        {
            get
            {
                if (!data.tileMask.IsValid) return false;
                return data.tileMask.IsExcludingTiles;
            }
        }

        public TileGrid(int width, int height)
        {
            this.width = width;
            this.height = height;

            data = new (width, height);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            allocationStack = Environment.StackTrace;
#endif
        }

        public static TileGrid DeepClone(TileGrid other)
        {
            TileGrid grid = new(other.width, other.height);
            if(other.IsDataValid) grid.data = other.data.DeepClone();
            return grid;
        }

        // Private Funcs
        public void SetGridData(NativeTileGrid newData) => data = newData;

        // Set Tile Type
        public bool SetTileId(int x, int y, int id)
        {
            return data.SetTileId(x, y, id);
        }

        public bool SetTileId(Vector2Int position, int id)
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
            return data.ContainsId(x, y, id);
        }

        public bool ContainsId(Vector2Int position, int id)
        {
            return ContainsId(position.x, position.y, id);
        }

        // Get Tile
        public Tile GetTile(int x, int y)
        {
            return data.GetTile(x, y);
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
            data.SetTile(x, y, toSet);
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
            return data.SetTileValue(x, y, value);
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
            data.RemoveMask();
        }

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
            data.CreateMask(includeList, excludeList);
            SetGridData(data);
        }

        public void ToggleMasked(bool on)
        {
            data.ToggleMasked(on);
        }

        public void AddExcludedPosition(int2 position)
        {
            data.AddExcludedPosition(position);
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
            return data.IsExcluding(position);
        }

        public bool IsExcluding(Tile tile)
        {
            return IsExcluding(tile.Int2);
        }

        public Vector2Int GetNearestPosition(int x, int y, int id)
        {
            int2 pos = data.GetNearestPosition(x, y, id);
            return new(pos.x, pos.y);
        }

        public Vector2Int GetNearestPosition(Vector2Int position, int id)
        {
            return GetNearestPosition(position.x, position.y, id);
        }

        public Vector2Int GetNearestPosition(int2 position, int id)
        {
            return GetNearestPosition(position.x, position.y, id);
        }

        public void ClearNumbers()
        {
            data.ClearNumbers();
        }

        public void ClearPositiveNumbers()
        {
           data.ClearPositiveNumbers();
        }

        public bool CanHaveTiles()
        {
            return data.CanHaveTiles();
        }

        public void Dispose()
        {
            data.Dispose();

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
            return data.GetEightNeighborTiles(tile);
        }

        public List<Tile> GetFourNeighborTiles(Tile tile)
        {
            return data.GetFourNeighborTiles(tile);
        }

        public Vector2Int GetRandomEdgePoint(AbstractRandom random)
        {
            int2 position = data.GetRandomEdgePoint(random);
            return new(position.x, position.y);
        }

        public int2 GetRandomEdgeInt2(AbstractRandom random)
        {
            return data.GetRandomEdgePoint(random);
        }

        public bool IsInBounds(int x, int y)
        {
            return data.IsInBounds(x, y);
        }

        public IEnumerator GetEnumerator()
        {
            return data.AsNativeArray().GetEnumerator();
        }

        public ref NativeTileGrid GetGridData() => ref data;

        public NativeTileGrid CloneGridData()
        {
            return NativeTileGrid.DeepClone(data);
        }

        public void OverrideGridData(NativeTileGrid _data)
        {
            data.Dispose();
            data = _data;
        }

        public void AddLayersLookups(Dictionary<int, LayerType> layerLookup)
        {
            data.AddLayersLookups(layerLookup);
        }
    }
}