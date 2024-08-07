using Sirenix.OdinInspector;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IDistanceConfig : IOccupanceConfig
    {
        [SerializeField, DefaultValue(DistanceType.Cardinal)] public DistanceType Distance { get; set; }

        [SerializeField, DefaultValue(false)] public bool FillOccupied { get; set; }
    }
}