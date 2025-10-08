using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IMazeConfig : IRoomConfig
    {
        [SerializeField, DefaultValue((int)TileDefaults.Wall_Cave)] public int WallTile { get; set; }

        [SerializeField, DefaultValue((int)TileDefaults.Wall_Object_NA)] public int HallwayTile { get; set; }
    }
}