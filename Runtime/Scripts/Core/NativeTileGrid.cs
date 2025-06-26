using System.Collections;
using System.Collections.Generic;
using System;
using Dalichrome.RandomGenerator.Random;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Collections.AllocatorManager;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    public struct NativeTileGrid : IDisposable
    {
        public readonly int width;
        public readonly int height;

        private NativeArray<Tile> tiles;

        private readonly Tile invalidTile;

        // Mask Variables
        private bool masked;
        public bool Masked { get { return masked; } }

        [ReadOnly] internal TileMask tileMask;
        [ReadOnly] private NativeParallelHashMap<int, LayerType> tileLayerLookup;
        [ReadOnly] private NativeParallelHashSet<int2> excludePositions;

        // Validity
        public bool IsValid { get; internal set; }

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

        private NativeTileGrid(int width, int height, bool allocateCollections)
        {
            this.width = width;
            this.height = height;

            Allocator allocator = Allocator.Persistent;

            tiles = allocateCollections
                ? new NativeArray<Tile>(width * height, allocator)
                : default;

            invalidTile = new Tile { IsValid = false };

            tileMask = allocateCollections
                ? new TileMask
                {
                    includeSet = new NativeParallelHashSet<int>(1, Allocator.Persistent),
                    excludeSet = new NativeParallelHashSet<int>(1, Allocator.Persistent),
                    IsValid = true
                }
                : default;
            masked = false;

            tileLayerLookup = allocateCollections
                ? TileTypeLayerLookup.CreateLookup(Allocator.Persistent)
                : default;

            excludePositions = allocateCollections
                ? new NativeParallelHashSet<int2>(64, allocator)
                : default;

            IsValid = true;
        }

        public NativeTileGrid(int width, int height)
    : this(width, height, true)
        {
            for (int i = 0; i < width * height; i++)
            {
                int2 pos = IndexToInt2(i);
                tiles[i] = new Tile(pos.x, pos.y);
            }
        }

        public static NativeTileGrid DeepClone(NativeTileGrid other)
        {
            NativeTileGrid gridData = new(other.width, other.height, false);

            Allocator allocator = Allocator.Persistent;

            // Tiles
            gridData.tiles = other.tiles.DeepClone(allocator);

            // Tile Mask
            gridData.masked = other.masked;
            gridData.tileMask = other.tileMask.IsValid
                ? other.tileMask.DeepClone()
                : new TileMask
                {
                    includeSet = new NativeParallelHashSet<int>(1, allocator),
                    excludeSet = new NativeParallelHashSet<int>(1, allocator),
                    IsValid = true
                };

            // Tile Layer Lookup
            gridData.tileLayerLookup = new NativeParallelHashMap<int, LayerType>(
                math.ceilpow2(other.tileLayerLookup.Count()), allocator);

            foreach (var kvp in other.tileLayerLookup)
                gridData.tileLayerLookup.TryAdd(kvp.Key, kvp.Value);

            // Exclude Positions
            gridData.excludePositions = new NativeParallelHashSet<int2>(
                math.ceilpow2(other.excludePositions.Count()), allocator);

            foreach (var pos in other.excludePositions)
                gridData.excludePositions.Add(pos);

            gridData.IsValid = true;

            return gridData;
        }

        private LayerType GetLayerFromId(int id)
        {
            if (tileLayerLookup.TryGetValue(id, out LayerType layer))
                return layer;
            
            return LayerType.NA;
        }

        private Tile GetTileFromNativeArray(int x, int y)
        {
            if (!tiles.IsCreated) return default;
            return tiles[PositionToIndex(x, y)];
        }

        private int PositionToIndex(int x, int y)
        {
            return (y * width) + x;
        }

        private int2 IndexToInt2(int index)
        {
            return new(index % width, index / width);
        }

        private bool CanModifyTile(Tile tile)
        {
            if (!Masked) return true;

            return tileMask.CanModifyTile(tile);
        }

        public bool SetTileId(int x, int y, int id)
        {
            if (!IsInBounds(x, y) || IsExcluding(x,y)) return false;

            Tile t = GetTileFromNativeArray(x, y);
            if (!t.IsValid || !CanModifyTile(t)) return false;

            t.SetId(id, GetLayerFromId(id));
            tiles[PositionToIndex(x, y)] = t;
            return true;
        }

        public bool SetTileId(int2 position, int id)
        {
            return SetTileId(position.x, position.y, id);
        }

        public bool SetTileId(Tile tile, int id)
        {
            tile.SetId(id, GetLayerFromId(id));
            return SetTileId(tile.Int2, id);
        }

        public bool ContainsId(int x, int y, int id)
        {
            Tile tile = GetTile(x, y);
            return tile.ContainsId(id);
        }

        public bool ContainsId(int2 position, int id)
        {
            return ContainsId(position.x, position.y, id);
        }

        public Tile GetTile(int x, int y)
        {
            if (!IsInBounds(x, y)) return invalidTile;

            return GetTileFromNativeArray(x, y);
        }

        public Tile GetTile(int2 position)
        {
            return GetTile(position.x, position.y);
        }

        public bool SetTile(int x, int y, Tile toSet)
        {
            if (!IsInBounds(x, y)) return false;

            Tile t = GetTileFromNativeArray(x, y);
            if (!t.IsValid || !CanModifyTile(t)) return false;

            t.SetLayersByTile(toSet);
            tiles[PositionToIndex(x, y)] = t;
            return true;
        }

        public bool SetTile(int2 position, Tile toSet)
        {
            return SetTile(position.x, position.y, toSet);
        }

        public bool SetTile(Tile oldTile, Tile toSet)
        {
            return SetTile(oldTile.Int2, toSet);
        }

        //Can still set the value for a masked tile
        public bool SetTileValue(int x, int y, int value)
        {
            if (!IsInBounds(x, y)) return false;

            Tile t = GetTileFromNativeArray(x, y);
            if (!t.IsValid) return false;

            t.SetValue(value);
            tiles[PositionToIndex(x, y)] = t;
            return true;
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

        public void RemoveMask()
        {
            masked = false;
            tileMask.Dispose();

            Allocator allocator = Allocator.Persistent;
            tileMask = new TileMask
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

                    if (IsInBounds(x1, y1) && GetTileFromNativeArray(x1, y1).ContainsId(id))
                    {
                        return new int2(x1, y1);
                    }

                    // Bottom edge (avoid duplicate check for middle row)
                    if (dy1 != dy2)
                    {
                        int x2 = x + dx;
                        int y2 = y + dy2;

                        if (IsInBounds(x2, y2) && GetTileFromNativeArray(x2, y2).ContainsId(id))
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
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SetTileValue(x, y, 0);
                }
            }
        }

        public void ClearPositiveNumbers()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = GetTileFromNativeArray(x, y);
                    if (tile.Value > 0)
                    {
                        SetTileValue(tile, 0);
                    }
                }
            }
        }

        public bool CanHaveTiles()
        {
            return width > 0 && height > 0;
        }

        public void Dispose()
        {
            if (!IsValid) return;

            tiles.Dispose();
            tileMask.Dispose();

            tileLayerLookup.Dispose();
            excludePositions.Dispose();

            IsValid = false;
        }

        public List<Tile> GetEightNeighborTiles(Tile tile)
        {
            List<Tile> tiles = new();
            int2 position = tile.Int2;
            tiles.Add(GetTile(position + Constants.Int2Left));
            tiles.Add(GetTile(position + Constants.Int2Left + Constants.Int2Up));
            tiles.Add(GetTile(position + Constants.Int2Up));
            tiles.Add(GetTile(position + Constants.Int2Right + Constants.Int2Up));
            tiles.Add(GetTile(position + Constants.Int2Right));
            tiles.Add(GetTile(position + Constants.Int2Right + Constants.Int2Down));
            tiles.Add(GetTile(position + Constants.Int2Down));
            tiles.Add(GetTile(position + Constants.Int2Left + Constants.Int2Down));

            tiles.RemoveAll(item => !item.IsValid);

            return tiles;
        }

        public List<Tile> GetFourNeighborTiles(Tile tile)
        {
            List<Tile> tiles = new();
            int2 position = tile.Int2;
            tiles.Add(GetTile(position + Constants.Int2Left));
            tiles.Add(GetTile(position + Constants.Int2Up));
            tiles.Add(GetTile(position + Constants.Int2Right));
            tiles.Add(GetTile(position + Constants.Int2Down));

            tiles.RemoveAll(item => !item.IsValid);

            return tiles;
        }

        public int2 GetRandomEdgePoint(AbstractRandom random)
        {
            int value = random.NextInt(4);

            if (value == 0) return new (width, random.NextInt(height));
            if (value == 1) return new (0, random.NextInt(height));
            if (value == 2) return new (random.NextInt(width), height);
            else return new (random.NextInt(width), 0);
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsInBounds(int2 pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public NativeArray<Tile> AsNativeArray()
        {
            return tiles;
        }

        public void AddLayersLookups(Dictionary<int, LayerType> layerLookup)
        {
            int needed = tileLayerLookup.Count() + layerLookup.Count;
            if (needed > tileLayerLookup.Capacity)
            {
                int newCap = math.ceilpow2(needed);  // power-of-two rule
                var bigger = new NativeParallelHashMap<int, LayerType>(newCap, Allocator.Persistent);

                // Copy existing pairs
                foreach (var kvp in tileLayerLookup)
                    bigger.TryAdd(kvp.Key, kvp.Value);

                tileLayerLookup.Dispose();
                tileLayerLookup = bigger;
            }

            foreach (var pair in layerLookup)
                tileLayerLookup[pair.Key] = pair.Value;
        }
    }
}