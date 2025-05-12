using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    public class MaskedTileGrid : TileGrid
    {
        public bool Masked { get { return masked; } }

        protected bool masked = true;
        protected TileMask tileMask;

        protected List<Vector2Int> excludePositionList = new();

        protected bool IsIncludingTiles { 
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

        public MaskedTileGrid(TileGrid other, TileMask mask) : this(other)
        {
            tileMask = mask;
            ToggleMasked(true);
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
            if (other.tileMask != null) tileGrid.tileMask = (TileMask) other.tileMask.Clone();

            return tileGrid;
        }

        protected bool CanModifyTile(Tile tile)
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
    }
}