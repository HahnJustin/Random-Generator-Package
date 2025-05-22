using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator
{
    public interface ITileInfoGrabber
    {
        public Sprite GetTileSprite(int id);

        public Color GetTileColor(int id);

        public TileBase GetTileBase(int ide);

        public GameObject GetGameObject(int id);

        public TileBase GetNumberTileBase(int value);
    }
}
