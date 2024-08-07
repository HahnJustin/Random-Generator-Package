using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Databases
{
    [CreateAssetMenu(menuName = "Databases/NumberSpriteDatabase")]
    public class NumberSpriteDatabase : Database<char, Sprite>
    {
        //public int TileMapSorting { get { return _tileMapSorting; } set { _tileMapSorting = value; } }
        //[SerializeField] private int _tileMapSorting = 10;
    }
}
