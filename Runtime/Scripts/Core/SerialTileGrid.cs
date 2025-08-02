using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Dalichrome.RandomGenerator.Random;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public class SerialTileGrid : ITileGrid, IEnumerable, IDisposable
    {
        public readonly int width;
        public readonly int height;

        private Tile[] tiles;

        private readonly Tile invalidTile;

        // Mask Variables
        private bool masked;
        public bool Masked { get { return masked; } }
        public ITileMask TileMask { get { return tileMask; } set { tileMask = (SerialTileMask)value; } }

        [ReadOnly] internal SerialTileMask tileMask;
        [ReadOnly] private Dictionary<int, LayerType> tileLayerLookup;
        [ReadOnly] private HashSet<int2> excludePositions;

        // Region Variables
        [ReadOnly] private HashSet<int2> regionPositions;
        private int2 regionMin;
        private int2 regionMax;
        private bool regionLimited;

        // Validity
        public bool IsValid { get; set; }

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

        public int2 Minimum
        {
            get { return regionMin; }
        }

        public int2 Maximum
        {
            get { return regionMax; }
        }

        public SerialTileGrid(int width, int height)
        {
            this.width = width;
            this.height = height;

            Allocator allocator = Allocator.Persistent;

            tiles = new Tile[width * height];

            invalidTile = new Tile { IsValid = false };

            tileMask = new();
            masked = false;

            tileLayerLookup = TileTypeLayerLookup.CreateLookup();

            excludePositions = new();

            regionPositions = new();

            regionMin = new int2(0, 0);
            regionMax = new int2(width - 1, height - 1);

            regionLimited = false;

            IsValid = true;

            for (int i = 0; i < width * height; i++)
            {
                int2 pos = IndexToInt2(i);
                tiles[i] = new Tile(pos.x, pos.y);
            }
        }

        public static SerialTileGrid DeepClone(SerialTileGrid other)
        {
            SerialTileGrid grid = new(other.width, other.height);

            Allocator allocator = Allocator.Persistent;

            // Tiles
            grid.tiles = other.tiles.DeepClone();

            // Tile Mask
            grid.masked = other.masked;
            grid.tileMask = other.tileMask.IsValid
                ? (SerialTileMask)other.tileMask.DeepClone()
                : new SerialTileMask
                {
                    includeList = new(other.tileMask.includeList),
                    excludeList = new(other.tileMask.excludeList),
                    IsValid = true
                };

            // Tile Layer Lookup
            grid.tileLayerLookup = other.tileLayerLookup;

            grid.excludePositions = new(other.excludePositions);
            grid.regionPositions = new(other.regionPositions);

            // Set Region Bounds
            grid.regionMax = other.regionMax;
            grid.regionMin = other.regionMin;

            grid.regionLimited = other.regionLimited;

            grid.IsValid = true;

            return grid;
        }

        private LayerType GetLayerFromId(int id)
        {
            if (tileLayerLookup.TryGetValue(id, out LayerType layer))
                return layer;

            return LayerType.NA;
        }

        private Tile GetTileFromNativeArray(int x, int y)
        {
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
            if (IsRestricted(x, y)) return false;

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
            if (IsRestricted(x, y)) return false;

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
            tileMask = new SerialTileMask
            {
                includeList = new (),
                excludeList = new (),
                IsValid = true
            };
        }

        public void CreateMask(List<int> includeList, List<int> excludeList)
        {
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
            return excludePositions.Contains(new(x, y));
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

            if (value == 0) return new(width, random.NextInt(height));
            if (value == 1) return new(0, random.NextInt(height));
            if (value == 2) return new(random.NextInt(width), height);
            else return new(random.NextInt(width), 0);
        }

        public RegionBounds GetRegionBounds()
        {
            var list = new List<int2>(regionPositions.Count);

            foreach (var pos in regionPositions)
                list.Add(pos);

            return new RegionBounds(regionMin, regionMax, list, this);
        }

        public void SetRegionBounds(int2 min, int2 max, List<int2> regionAddPositions)
        {
            regionPositions.Clear();
            regionMin = min;
            regionMax = max;
            regionLimited = true;

            foreach (var pos in regionAddPositions)
                AddRegionPosition(pos);
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
            regionPositions.Add(new(x, y));
        }

        public void AddRegionPosition(int2 pos)
        {
            regionPositions.Add(pos);
        }

        public bool IsInRegion(Tile tile)
        {
            return IsInRegion(tile.Int2);
        }

        public bool IsInRegion(int2 pos)
        {
            if (!regionLimited) return true;
            return math.all(pos >= regionMin) &&
                   math.all(pos <= regionMax) &&
                   regionPositions.Contains(pos);
        }

        public bool IsInRegion(int x, int y)
        {
            return IsInRegion(new int2(x, y));
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsInBounds(int2 pos)
        {
            return IsInBounds(pos.x, pos.y);
        }

        public bool IsRestricted(int x, int y)
        {
            return !IsInBounds(x, y) || IsExcluding(x, y) || !IsInRegion(x, y);
        }

        public bool IsRestricted(int2 pos)
        {
            return IsRestricted(pos.x, pos.y);
        }

        public bool IsInsideMask(int2 pos)
        {
            return IsInsideMask(pos.x, pos.y);
        }

        public bool IsInsideMask(int x, int y)
        {
            return tileMask.CanModifyTile(GetTile(x, y));
        }

        public NativeArray<Tile> AsNativeArray()
        {
            NativeArray < Tile > nativeTiles = new(width * height, Allocator.Persistent);

            int i = 0;
            foreach (Tile tile in tiles)
            {
                nativeTiles[i] = tile;
                i++;
            }
            return nativeTiles;
        }

        public IEnumerable<Tile> GetTiles()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int2 pos = new(x, y);
                    yield return GetTileFromNativeArray(x, y);
                }
            }
        }

        public IEnumerable<Tile> GetRegionTiles()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
            {
                for (int x = regionMin.x; x <= regionMax.x; x++)
                {
                    int2 pos = new(x, y);
                    if (regionPositions.Contains(pos))
                        yield return GetTileFromNativeArray(x, y);
                }
            }
        }

        public IEnumerable<Tile> GetRegionGrid()
        {
            for (int y = regionMin.y; y <= regionMax.y; y++)
            {
                for (int x = regionMin.x; x <= regionMax.x; x++)
                {
                    yield return GetTileFromNativeArray(x, y);
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (regionLimited)
            {
                return GetRegionTiles().GetEnumerator();
            }
            else return GetTiles().GetEnumerator();
        }

        public void AddLayersLookups(Dictionary<int, LayerType> layerLookup)
        {
            foreach (var pair in layerLookup)
                tileLayerLookup[pair.Key] = pair.Value;
        }


        public void SetAllTiles(Tile[] tileArray)
        {
            for (int i = 0; i < width * height; i++)
            {
                tiles[i] = tileArray[i];
            }
        }
    }
}