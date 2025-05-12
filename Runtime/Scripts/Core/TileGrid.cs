using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Core
{
    [Serializable]
    public class TileGrid : IEnumerable
    {
        public readonly int width;
        public readonly int height;

        public Vector2Int Center { get { return new Vector2Int(Mathf.Clamp(width/2,0,width), Mathf.Clamp(height /2, 0, height)); } }

        public BoundsInt Bounds { get { return new(new Vector3Int(0, 0, 0), new Vector3Int(width, height, 1)); } } 

        protected Tile[,] grid;

        protected readonly Tile invalidTile = new (){ IsValid = false };

        // Mask Variables
        protected bool masked = true;
        public bool Masked { get { return masked; } }

        protected TileMask tileMask;
        protected List<Vector2Int> excludePositionList = new();

        protected bool IsIncludingTiles
        {
            get
            {
                if (tileMask == null) return false;
                return tileMask.includeList.Count > 0;
            }
        }
        protected bool IsExcludingTiles
        {
            get
            {
                if (tileMask == null) return false;
                return tileMask.excludeList.Count > 0;
            }
        }

        public TileGrid(TileGrid other)
        {
            width = other.width;
            height = other.height;

            grid = other.grid;
        }

        public TileGrid(int width, int height)
        {
            this.width = width;
            this.height = height;

            grid = new Tile[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    grid[i, j] = new(i, j);
                }
            }
        }
        private bool CanModifyTile(Tile tile)
        {
            if (excludePositionList.Contains(tile.Position))
            {
                return false;
            }

            if (!Masked || tileMask == null) return true;

            bool included = false;
            bool excluded = false;

            foreach (TileType type in tileMask.includeList)
            {
                if (tile.ContainsType(type))
                {
                    included = true;
                    break;
                }
            }

            foreach (TileType type in tileMask.excludeList)
            {
                if (tile.ContainsType(type))
                {
                    excluded = true;
                    break;
                }
            }

            if (IsExcludingTiles && excluded) return false;
            else if (IsIncludingTiles && included) return true;
            else return !IsIncludingTiles;
        }

        public static TileGrid DeepClone(TileGrid other)
        {
            TileGrid tileGrid = new(other.width, other.height);
            tileGrid.grid = other.grid.DeepClone();

            tileGrid.masked = other.masked;
            if (other.tileMask != null) tileGrid.tileMask = (TileMask)other.tileMask.Clone();

            return tileGrid;
        }

        public bool SetTileType(int x, int y, TileType type)
        {
            if (!IsInBounds(x, y)) return false;

            Tile t = grid[x, y];
            if (!t.IsValid || !CanModifyTile(t)) return false;

            t.SetType(type);
            grid[x, y] = t;
            return true;
        }

        public bool SetTileType(Vector2Int position, TileType type)
        {
            return SetTileType(position.x, position.y, type);
        }

        public bool SetTileType(Tile tile, TileType type)
        {
            return SetTileType(tile.Position, type);
        }

        public bool ContainsType(int x, int y, TileType type)
        {
            Tile tile = GetTile(x, y);
            return tile.ContainsType(type);
        }

        public bool ContainsType(Vector2Int position, TileType type)
        {
            return ContainsType(position.x, position.y, type);
        }

        public Tile GetTile(int x, int y)
        {
            if (!IsInBounds(x, y)) return invalidTile;

            return grid[x, y];
        }
        public Tile GetTile(Vector2Int position)
        {
            return GetTile(position.x, position.y);
        }

        public bool SetTile(int x, int y, Tile toSet)
        {
            if (!IsInBounds(x, y)) return false;

            Tile t = grid[x, y];
            if (!t.IsValid || !CanModifyTile(t)) return false;

            t.SetTypes(toSet);
            grid[x, y] = t;
            return true;
        }

        public bool SetTile(Vector2Int position, Tile toSet)
        {
            return SetTile(position.x, position.y, toSet);
        }

        public bool SetTile(Tile oldTile, Tile toSet)
        {
            return SetTile(oldTile.Position, toSet);
        }

        //Can still set the value for a masked tile
        public bool SetTileValue(int x, int y, int value)
        {
            if (!IsInBounds(x, y)) return false;

            Tile t = grid[x, y];
            if (!t.IsValid) return false;

            t.SetValue(value);
            grid[x, y] = t;
            return true;
        }

        public bool SetTileValue(Vector2Int position, int value)
        {
            return SetTileValue(position.x, position.y, value);
        }

        public bool SetTileValue(Tile tile, int value)
        {
            return SetTileValue(tile.x, tile.y, value);

        }

        public void RemoveMask()
        {
            masked = false;
            tileMask = null;
        }

        public void AddMask(TileMask mask)
        {
            tileMask = mask;
        }

        public void ToggleMasked(bool on)
        {
            this.masked = on;
        }

        public void AddExcludedPosition(Vector2Int position)
        {
            excludePositionList.Add(position);
        }

        public bool IsExcluding(Vector2Int position)
        {
            return excludePositionList.Contains(position);
        }

        public bool IsExcluding(Tile tile)
        {
            return IsExcluding(tile.Position);
        }

        public Vector2Int GetNearestPosition(int x, int y, TileType type)
        {
            // Iterate through all distances from the center
            for (int d = 1; d < Mathf.Max(height, width); d++)
            {
                // Check all positions at distance `d`
                for (int dx = -d; dx <= d; dx++)
                {
                    int dy1 = d - Mathf.Abs(dx); // Top and bottom edges
                    int dy2 = -dy1;

                    // Top edge
                    int x1 = x + dx;
                    int y1 = y + dy1;

                    if (IsInBounds(x1, y1) && grid[x1, y1].ContainsType(type))
                    {
                        return new Vector2Int(x1, y1);
                    }

                    // Bottom edge (avoid duplicate check for middle row)
                    if (dy1 != dy2)
                    {
                        int x2 = x + dx;
                        int y2 = y + dy2;

                        if (IsInBounds(x2, y2) && grid[x2, y2].ContainsType(type))
                        {
                            return new Vector2Int(x2, y2);
                        }
                    }
                }
            }
            return Constants.OutsideGridVectorInt;
        }

        public Vector2Int GetNearestPosition(Vector2Int position, TileType type)
        {
            return GetNearestPosition(position.x, position.y, type);
        }

        public void ClearNumbers()
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    SetTileValue(x,y,0);
                }
            }
        }

        public void ClearPositiveNumbers()
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    Tile tile = grid[x, y];
                    if (tile.Value > 0)
                    {
                        SetTileValue(tile, 0);
                    }
                }
            }
        }

        public bool IsValid()
        {
            return width > 0 && height > 0;
        }

        public IEnumerator GetEnumerator()
        {
            return grid.GetEnumerator();
        }

        public List<Tile> GetEightNeighborTiles(Tile tile)
        {
            List<Tile> tiles = new();
            Vector2Int position = tile.Position;
            tiles.Add(GetTile(position + Vector2Int.left));
            tiles.Add(GetTile(position + Vector2Int.left + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.right + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.right));
            tiles.Add(GetTile(position + Vector2Int.right + Vector2Int.down));
            tiles.Add(GetTile(position + Vector2Int.down));
            tiles.Add(GetTile(position + Vector2Int.left + Vector2Int.down));

            tiles.RemoveAll(item => !item.IsValid);

            return tiles;
        }

        public List<Tile> GetFourNeighborTiles(Tile tile)
        {
            List<Tile> tiles = new();
            Vector2Int position = tile.Position;
            tiles.Add(GetTile(position + Vector2Int.left));
            tiles.Add(GetTile(position + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.right));
            tiles.Add(GetTile(position + Vector2Int.down));

            tiles.RemoveAll(item => !item.IsValid);

            return tiles;
        }

        public Vector2Int GetRandomEdgePoint(AbstractRandom random)
        {
            int value = random.NextInt(4);

            if (value == 0) return new Vector2Int(width, random.NextInt(height));
            if (value == 1) return new Vector2Int(0, random.NextInt(height));
            if (value == 2) return new Vector2Int(random.NextInt(width), height);
            else return new Vector2Int(random.NextInt(width), 0);
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1);
        }
    }
}