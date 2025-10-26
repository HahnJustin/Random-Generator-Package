using System.Collections.Generic;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomFillGenerator : AbstractGenerator<RoomFillConfig>
    {
        private RoomUtil util;

        protected new RoomFillConfig config;

        public RoomFillGenerator(RoomFillConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            //Culling Small Rooms, Skips calculation on biggest room otherwise (i >= 0)
            List<Room> roomList = util.LargestFirstRoomList;
            roomList.Sort();
            for (int i = roomList.Count - 1; i > 0; i--)
            {
                Room room = roomList[i];
                if ((room.Count <= config.MinimumRoomSize && config.FillType == RoomFillType.Size_Fill) || 
                    (util.Rooms.Count > config.RoomLimit && config.FillType == RoomFillType.Fill_Until_X_Left ))
                {
                    int2 pos = room.GetFirstPosition();
                    util.RoomFill(pos.x, pos.y, null);
                    util.Rooms.Remove(room.Value);
                    if (config.DebugRooms)
                    {
                        foreach (int2 pos2 in room)
                        {
                            TileGrid.SetTileId(pos2, (int)TileDefaults.Debug_Path_Red);
                        }
                    }
                }
                else break;
            }
            return input;
        }
    }
}
