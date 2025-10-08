using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class RoomSplitterConfig : AbstractRegionSplitterConfig, IRoomConfig
    {
        public override string IconName => "roomsplitter-icon";

        public RoomSplitterConfig()
        {
            _description = StringType.Description_Splitter_Room;
        }

        public override SplitterType Type { get { return SplitterType.Room; } }

        public int MinimumRoomSize { get { return _minimumRoomSize; } set { _minimumRoomSize = value; } }
        [SerializeField] private int _minimumRoomSize = 1;

        public int MaximumRoomSize { get { return _maximumRoomSize; } set { _maximumRoomSize = value; } }
        [SerializeField] private int _maximumRoomSize = int.MaxValue;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected LayerType _occupyLayer = LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}