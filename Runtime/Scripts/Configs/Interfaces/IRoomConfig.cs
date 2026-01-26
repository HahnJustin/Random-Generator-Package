using Dalichrome.RandomGenerator.Core;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IRoomConfig : IOccupanceConfig
    {
        [DefaultValue(false)] public bool ConfigureFillTile { get;}
        [SerializeField, DefaultValue((int)TileDefaults.Wall_Cave), Condition("ConfigureFillTile", true)] public int FillTile { get; set; }
    }
}