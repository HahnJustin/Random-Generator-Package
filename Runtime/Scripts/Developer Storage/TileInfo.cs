using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator;

[Serializable]
public class TileInfo
{
    public LayerType layer;
    public Sprite sprite_16;
    public Sprite sprite_8;
    public Sprite sprite_4;
    public Sprite sprite_menu;
    public Color color;
    public TileBase tile_16;
    public TileBase tile_8;
    public TileBase tile_4;
}
