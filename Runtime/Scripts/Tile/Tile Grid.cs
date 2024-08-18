using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator
{
    [Serializable]
    public class TileGrid : IEnumerable
    {
        public readonly int width;
        public readonly int height;

        public Vector2Int Center { get { return new Vector2Int(Mathf.Clamp(width/2,0,width), Mathf.Clamp(height /2, 0, height)); } }

        protected Tile[,] grid;

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

        public static TileGrid DeepClone(TileGrid other)
        {
            TileGrid tileGrid = new(other.width, other.height);
            tileGrid.grid = other.grid.DeepClone();
            return tileGrid;
        }

        public bool SetTileType(int x, int y, TileType type)
        {
            Tile tile = GetTile(x, y);
            return SetTileType(tile, type);
        }

        public bool SetTileType(Tile tile, TileType type)
        {
            tile.SetType(type);
            return true;
        }

        public bool SetTileType(Vector2Int position, TileType type)
        {
            SetTileType(position.x, position.y, type);
            return true;
        }

        public void SetTileValue(int x, int y, int value)
        {
            Tile tile = GetTile(x, y);

            tile.Value = value;
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
            if (x >= width || y >= height ||
                x < 0 || y < 0)
            {
                return null;
            }

            return grid[x, y];
        }
        public Tile GetTile(Vector2Int position)
        {
            return GetTile(position.x, position.y);
        }

        public bool SetTile(int x, int y, Tile toSet)
        {
            Tile tile = GetTile(x, y);
            tile.SetTypes(toSet);
            return true;
        }

        public bool SetTile(Vector2Int position, Tile toSet)
        {
            SetTile(position.x, position.y, toSet);
            return true;
        }

        public Vector2Int GetNearestPosition(int x, int y, TileType type)
        {
            // Check point (xs, ys)
            for (int d = 1; d < Mathf.Max(height, width); d++)
            {
                for (int i = 0; i < d + 1; i++)
                {
                    int x1 = x - d + i;
                    int y1 = y - i;

                    if (x1 < grid.GetLength(0) && x1 > -1 &&
                        y1 < grid.GetLength(1) && y1 > -1 &&
                        grid[x1, y1].ContainsType(type))
                    {
                        return new Vector2Int(x1, y1);
                    }

                    int x2 = x + d - i;
                    int y2 = y + i;

                    if (x2 < grid.GetLength(0) && x2 > -1 &&
                        y2 < grid.GetLength(1) && y2 > -1 &&
                        grid[x2, y2].ContainsType(type))
                    {
                        return new Vector2Int(x2, y2);
                    }
                }

                for (int i = 1; i < d; i++)
                {
                    int x1 = x - i;
                    int y1 = y + d - i;

                    if (x1 < grid.GetLength(0) && x1 > -1 &&
                        y1 < grid.GetLength(1) && y1 > -1 &&
                        grid[x1, y1].ContainsType(type))
                    {
                        return new Vector2Int(x1, y1);
                    }

                    int x2 = x + i;
                    int y2 = y - d + i;

                    if (x2 < grid.GetLength(0) && x2 > -1 &&
                        y2 < grid.GetLength(1) && y2 > -1 &&
                        grid[x2, y2].ContainsType(type))
                    {
                        return new Vector2Int(x2, y2);
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
                    Tile tile = grid[x, y];
                    tile.Value = 0;
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
                    if (tile.Value > 0) tile.Value = 0;
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
            Vector2Int position = tile.Vector;
            tiles.Add(GetTile(position + Vector2Int.left));
            tiles.Add(GetTile(position + Vector2Int.left + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.right + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.right));
            tiles.Add(GetTile(position + Vector2Int.right + Vector2Int.down));
            tiles.Add(GetTile(position + Vector2Int.down));
            tiles.Add(GetTile(position + Vector2Int.left + Vector2Int.down));

            tiles.RemoveAll(item => item == null);

            return tiles;
        }

        public List<Tile> GetFourNeighborTiles(Tile tile)
        {
            List<Tile> tiles = new();
            Vector2Int position = tile.Vector;
            tiles.Add(GetTile(position + Vector2Int.left));
            tiles.Add(GetTile(position + Vector2Int.up));
            tiles.Add(GetTile(position + Vector2Int.right));
            tiles.Add(GetTile(position + Vector2Int.down));

            tiles.RemoveAll(item => item == null);

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
    }
}