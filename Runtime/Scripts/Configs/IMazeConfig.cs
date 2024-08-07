using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IMazeConfig : IRoomConfig
    {
        [SerializeField, DefaultValue(TileType.Wall_Cave)] public TileType WallTile { get; set; }

        [SerializeField, DefaultValue(TileType.Wall_Object_NA)] public TileType HallwayTile { get; set; }
    }
}