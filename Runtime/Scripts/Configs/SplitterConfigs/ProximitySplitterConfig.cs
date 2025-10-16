using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class ProximitySplitterConfig : AbstractRegionSplitterConfig, IOccupanceConfig
    {
        public override string IconName => "proximitysplitter-icon";

        public ProximitySplitterConfig()
        {
            _description = StringType.Description_Splitter_Proximity;
        }

        public override SplitterType Type { get { return SplitterType.Proximity; } }
        [TileDisplay] public List<int> Tiles { get { return _tiles; } set { _tiles = value; } }
        [TileDisplay, SerializeField] private List<int> _tiles = new() { (int)TileDefaults.Wall_Cave };

        public int MinimumRadiusSize { get { return _minimumRadiusSize; } set { _minimumRadiusSize = value; } }
        [SerializeField] private int _minimumRadiusSize = 1;

        public int MaximumRadiusSize { get { return _maximumRadiusSize; } set { _maximumRadiusSize = value; } }
        [SerializeField] private int _maximumRadiusSize = 3;

        public bool UseOccupance { get { return _useOccupance; } set { _useOccupance = value; } }
        [SerializeField] private bool _useOccupance = false;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Default;

        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA)] public int OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected int _occupyLayer = (int)LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}