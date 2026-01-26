using System;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IMetaConditionConfig : IOccupanceConfig
    {
        [SerializeField, DefaultValue(DistanceType.Cardinal)] public string Distance { get; set; }

        [SerializeField, DefaultValue(false)] public bool FillOccupied { get; set; }
    }
}