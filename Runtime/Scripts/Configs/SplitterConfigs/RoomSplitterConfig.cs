using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class RoomSplitterConfig : AbstractRegionSplitterConfig
    {
        public RoomSplitterConfig()
        {
            _description = StringType.Description_Splitter_Room;
        }

        public override SplitterType Type { get { return SplitterType.Room; } }

        public int MinimumRoomSize { get { return _minimumRoomSize; } set { _minimumRoomSize = value; } }
        [SerializeField] private int _minimumRoomSize = 1;

        public int MaximumRoomSize { get { return _maximumRoomSize; } set { _maximumRoomSize = value; } }
        [SerializeField] private int _maximumRoomSize = int.MaxValue;
    }
}