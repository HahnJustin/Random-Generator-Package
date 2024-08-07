using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dalichrome.RandomGenerator
{
    public interface ITileInfoGrabber
    {
        public LayerType GetLayerOfTile(TileType type);

        public Sprite GetTileSprite(TileType type);

        public Color GetTileColor(TileType type);

        public TileBase GetTileBase(TileType type);

        public TileBase GetNumberTileBase(int value);
    }
}
