using System.Collections.Generic;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomFillGenerator : RoomGenerator
    {
        protected new RoomFillConfig config;

        public RoomFillGenerator(RoomFillConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            //Culling Small Rooms, Skips calculation on biggest room otherwise (i >= 0)
            List<Room> roomList = LargestFirstRoomList;
            roomList.Sort();
            for (int i = roomList.Count - 1; i > 0; i--)
            {
                Room room = roomList[i];
                if ((room.Count <= config.MinimumRoomSize && config.FillType == RoomFillType.Size_Fill) || 
                    (Rooms.Count > config.RoomLimit && config.FillType == RoomFillType.Fill_Until_X_Left ))
                {
                    Tile tile = room.GetFirstTile();
                    RoomFill(tile.x, tile.y, null);
                    Rooms.Remove(room.Value);
                    if (config.DebugRooms)
                    {
                        foreach (Tile roomTile in room)
                        {
                            roomTile.SetType(TileType.Debug_Path_Red);
                        }
                    }
                }
                else break;
            }
        }
    }
}
