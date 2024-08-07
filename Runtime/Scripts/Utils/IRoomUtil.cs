using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Utils
{
    public interface IRoomUtil
    {
        public abstract List<Room> SortBySmallest(List<Room> rooms);

        public abstract void RoomFill(TileGrid grid, int x, int y, Room room, bool useNumbers);
    }
}
