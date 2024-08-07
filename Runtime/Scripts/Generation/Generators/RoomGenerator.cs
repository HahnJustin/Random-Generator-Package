using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class RoomGenerator : OccupanceGenerator, IRoomUtil
    {
        private RoomUtil util;

        protected new IRoomConfig config;

        protected Dictionary<int, Room> Rooms { get { return util.Rooms; } }

        protected List<Room> RoomList { get { return util.RoomList; } }

        protected List<Room> LargestFirstRoomList 
        { 
            get { return util.LargestFirstRoomList; } 
        }

        protected List<Room> SmallestFirstRoomList 
        { 
            get { return util.SmallestFirstRoomList; } 
        }

        protected RoomGenerator(IRoomConfig config) : base(config)
        {
            this.config = config;
            util = new RoomUtil(config);
            AddUtil(util);
        }

        public List<Room> SortBySmallest(List<Room> rooms)
        {
            return util.SortBySmallest(rooms);
        }

        public void RoomFill(int x, int y, Room room, bool useNumbers = false)
        {
            util.RoomFill(x, y, room, useNumbers);
        }

        public void RoomFill(TileGrid grid, int x, int y, Room room, bool useNumbers)
        {
            util.RoomFill(TileGrid, x, y, room, useNumbers);
        }
    }
}