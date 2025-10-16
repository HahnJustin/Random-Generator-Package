using System;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IOccupanceConfig
    {
        [SerializeField, DefaultValue(OccupanceType.Default)] public OccupanceType Occupance { get; set; }

        [SerializeField, DefaultValue((int)LayerType.Wall), Condition("Occupance", OccupanceType.Layer_Not_NA)] public int OccupyLayer { get; set; }

        [SerializeField, DefaultValue((int)TileDefaults.Wall_Cave), Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get; set; }

        [SerializeField, DefaultValue(false)] public bool InvertOccupance { get; set; }
    }
}