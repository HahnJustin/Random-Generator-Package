using Sirenix.OdinInspector;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IOccupanceConfig
    {
        [SerializeField, DefaultValue(OccupanceType.Wall_Obj_Not_NA)] public OccupanceType Occupance { get; set; }

        [SerializeField, DefaultValue(LayerType.Wall), Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get; set; }

        [SerializeField, DefaultValue(TileType.Wall_Cave), Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get; set; }

        [SerializeField, DefaultValue(false)] public bool InvertOccupance { get; set; }
    }
}