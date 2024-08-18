using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dalichrome.RandomGenerator
{
    public interface ITileInfoGrabber
    {
        public Sprite GetTileSprite(TileType type);

        public Color GetTileColor(TileType type);

        public TileBase GetTileBase(TileType type);

        public GameObject GetGameObject(TileType type);

        public TileBase GetNumberTileBase(int value);
    }
}
