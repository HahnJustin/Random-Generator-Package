using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class RoomFillConfig : AbstractGeneratorConfig, IRoomConfig
    {
        public RoomFillConfig()
        {
            _description = StringType.Description_Generator_RoomFill;
        }

        public override GeneratorType Type { get { return GeneratorType.Room_Fill; } }

        public RoomFillType FillType { get { return _roomFill; } set { _roomFill = value; } }
        [SerializeField] private RoomFillType _roomFill = RoomFillType.Size_Fill;
        [Condition("FillType", RoomFillType.Fill_Until_X_Left)] public int RoomLimit { get { return _roomLimit; } set { _roomLimit = value; } }
        [SerializeField] private int _roomLimit = 1;

        [Condition("FillType", RoomFillType.Size_Fill)] public int MinimumRoomSize { get { return _minimumRoomSize; } set { _minimumRoomSize = value; } }
        [SerializeField] private int _minimumRoomSize = 10;

        public bool DebugRooms { get { return _debugRooms; } set { _debugRooms = value; } }
        [SerializeField] private bool _debugRooms = false;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Wall_Obj_Not_NA;

        [Condition("Occupance", OccupanceType.Layer_Not_NA)] public LayerType OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [SerializeField, ShowIf("Occupance", OccupanceType.Layer_Not_NA)] protected LayerType _occupyLayer = LayerType.Wall;

        [Condition("Occupance", OccupanceType.Contains_A)] public TileType TileA { get { return _tileA; } set { _tileA = value; } }
        [SerializeField, ShowIf("Occupance", OccupanceType.Contains_A)] protected TileType _tileA = TileType.Wall_Cave;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}