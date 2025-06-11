using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class RoomSplitterConfig : AbstractRegionSplitterConfig
    {
        public RoomSplitterConfig()
        {
            _description = StringType.Description_Splitter_Room;
        }

        public override SplitterType Type { get { return SplitterType.Room; } }
    }
}