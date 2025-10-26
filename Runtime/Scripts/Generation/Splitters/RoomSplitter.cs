using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Data;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomSplitter : AbstractSplitter<RoomSplitterConfig>
    {
        private RoomUtil util;
        public RoomSplitter(RoomSplitterConfig config, int outputs) : base(config, outputs)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override RegionSplits Split(Generation generation)
        {
            RegionBounds bounds = generation.Grid.GetRegionBounds();
            RegionSplits regionSplits = new(generation);

            // Culling Rooms
            List<Room> roomList = util.LargestFirstRoomList;
            for (int i = roomList.Count - 1; i >= 0; i--)
            {
                Room room = roomList[i];
                if ((room.Count < config.MinimumRoomSize) || (room.Count > config.MaximumRoomSize))
                    
                {
                    roomList.Remove(room);
                }
            }

            // Adding Rooms to RegionSplits
            foreach (Room room in roomList) 
            {
                regionSplits.AddRegion(room.ToRegionBounds(TileGrid));
            }

            return regionSplits;
        }
    }
}
