using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator
{
    public class MaskedTileGrid : TileGrid
    {
        public bool Masked { get { return masked; } }

        protected bool masked = false;
        protected List<TileType> includeList = new();
        protected List<TileType> excludeList = new();

        protected List<Vector2Int> excludePositionList = new();

        protected bool IsIncludingTiles { get { return includeList.Count > 0; } }
        protected bool IsExcludingTiles { get { return excludeList.Count > 0; } }

        public MaskedTileGrid(TileGrid other, AbstractGeneratorConfig config) : this(other)
        {
            SetMask(config);
        }

        public MaskedTileGrid(TileGrid other) : base(other)
        {

        }

        public MaskedTileGrid(int width, int height) : base(width, height)
        {

        }

        public static MaskedTileGrid DeepClone(MaskedTileGrid other)
        {
            MaskedTileGrid tileGrid = new(other.width, other.height);
            tileGrid.grid = other.grid.DeepClone();

            tileGrid.masked = other.masked;
            tileGrid.includeList = new(other.includeList);
            tileGrid.excludeList = new(other.excludeList);

            return tileGrid;
        }

        protected bool CanModifyTile(Tile tile)
        {
            if (excludePositionList.Contains(tile.Vector))
            {
                return false;
            }

            if (!Masked) return true;

            bool included = false;
            bool excluded = false;

            foreach (TileType type in includeList)
            {
                if (tile.ContainsType(type))
                {
                    included = true;
                    break;
                }
            }

            foreach (TileType type in excludeList)
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

        public new bool SetTileType(int x, int y, TileType type)
        {
            Tile tile = GetTile(x, y);

            if (CanModifyTile(tile))
            {
                return base.SetTileType(x, y, type);
            }
            return false;
        }

        public new bool SetTileType(Tile tile, TileType type)
        {
            if (CanModifyTile(tile))
            {
                return base.SetTileType(tile, type);
            }
            return false;
        }

        public new bool SetTileType(Vector2Int position, TileType type)
        {
            return SetTileType(position.x, position.y, type);
        }

        public new bool SetTile(int x, int y, Tile toSet)
        {
            Tile tile = GetTile(x, y);

            if (CanModifyTile(tile))
            {
                return base.SetTile(x, y, toSet);
            }
            return false;
        }

        public new bool SetTile(Vector2Int position, Tile toSet)
        {
            return SetTile(position.x, position.y, toSet);
        }

        public void RemoveMask()
        {
            masked = false;
            if (includeList != null) includeList = new();
            if (excludeList != null) excludeList = new();
        }

        public void SetMask(AbstractGeneratorConfig config)
        {
            this.masked = config.Masked;
            this.includeList = config.IncludeList;
            this.excludeList = config.ExcludeList;
        }

        public void AddExcludedPosition(Vector2Int position)
        {
            excludePositionList.Add(position);
        }
    }
}