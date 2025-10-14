using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public class RoomUtil : OccupanceUtil, IRoomUtil, IInitializableUtil
    {
        protected new IRoomConfig config;

        protected Dictionary<int, Room> rooms;

        public Dictionary<int, Room> Rooms { get { return rooms; } }

        public List<Room> RoomList { get { return rooms.Values.ToList(); } }

        public List<Room> LargestFirstRoomList
        {
            get
            {
                List<Room> roomList = rooms.Values.ToList();
                roomList.Sort();
                return roomList;
            }
        }

        public List<Room> SmallestFirstRoomList
        {
            get
            {
                List<Room> roomList = rooms.Values.ToList();
                roomList.Sort(new SmallestRoomFirstSort());
                return roomList;
            }
        }

        private int currentRoomNumber = -1;

        private class SmallestRoomFirstSort : IComparer<Room>
        {
            public int Compare(Room x, Room y)
            {
                if (x.Count < y.Count) return 1;
                else if (x.Count > y.Count) return -1;
                return 0;
            }
        }

        public RoomUtil(IRoomConfig config) : base(config)
        {
            this.config = config;
        }

        private bool EnqueueIfMatches(TileGrid grid, Queue<Tuple<int2, int>> queue, int x, int y, int number)
        {
            // Outta bounds
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height)
            {
                return true;
            }

            // Isn't Occupied
            if (IsOccupied(x, y) <= 0)
            {
                queue.Enqueue(new(new int2(x, y), number));
                return false;
            }
            return true;
        }

        protected void CreateRooms()
        {
            tileGrid.ClearNumbers();
            rooms = new();
            for (int x = 0; x < tileGrid.width; x++)
            {
                for (int y = 0; y < tileGrid.height; y++)
                {

                    if (tileGrid.IsInRegion(x, y) && IsOccupied(x,y) <= 0 && tileGrid.GetTileValue(x,y) == 0)
                    {
                        Room newRoom = new(currentRoomNumber);
                        RoomFill(tileGrid, x, y, newRoom, true);
                        rooms.Add(newRoom.Value, newRoom);
                        currentRoomNumber -= 1;
                    }
                }
            }
        }

        public List<Room> SortBySmallest(List<Room> rooms)
        {
            rooms.Sort(new SmallestRoomFirstSort());
            return rooms;
        }

        public void RoomFill(int x, int y, Room room, bool useNumbers = false)
        {
            RoomFillHelper( tileGrid, x, y, room, useNumbers);
        }

        public void RoomFill(TileGrid grid, int x, int y, Room room, bool useNumbers = false)
        {
            RoomFillHelper(grid, x, y, room, useNumbers);
        }

        private void RoomFillHelper(TileGrid grid, int x, int y, Room room, bool useNumbers, bool decreaseNumbers = false, int number = -1)
        {
            Queue<Tuple<int2, int>> queue = new Queue<Tuple<int2, int>>();
            queue.Enqueue(new(new int2(x, y), number));

            while (queue.Any())
            {
                Tuple<int2, int> tuple = queue.Dequeue();
                int2 point = tuple.Item1;
                int currentNum = tuple.Item2;

                // Is Occupied
                if (!grid.IsInRegion(point) || IsOccupied(point) >= 1 || (grid.GetTileValue(point) < 0 && useNumbers))
                    continue;
                if (room != null)
                {
                    room.AddPosition(point);
                }

                if (decreaseNumbers && useNumbers) tileGrid.SetTileValue(point, currentNum);
                else if (useNumbers) tileGrid.SetTileValue(point, currentRoomNumber);
                else Fill(point);

                if (decreaseNumbers) currentNum -= 1;

                bool leftOccupied = EnqueueIfMatches(grid, queue, point.x - 1, point.y, currentNum);
                bool rightOccupied = EnqueueIfMatches(grid, queue, point.x + 1, point.y, currentNum);
                bool downOccupied = EnqueueIfMatches(grid, queue, point.x, point.y - 1, currentNum);
                bool upOccupied = EnqueueIfMatches(grid, queue, point.x, point.y + 1, currentNum);

                if ((leftOccupied || rightOccupied || downOccupied || upOccupied) && room != null)
                {
                    room.AddEdge(point);
                }
            }
        }

        private void RoomCreateRecurse(TileGrid grid, int x, int y, Room room, int firstNumber, bool lowerNumber = false, int number = -1)
        {
            // Base cases
            if (x < 0 || x >= grid.width ||
                y < 0 || y >= grid.height)
                return;

            // Is occupied
            if (IsOccupied(x,y) >= 1 || tileGrid.GetTileValue(x,y) <= firstNumber)
                return;

            //Add
            if (room != null)
            {
                room.AddPosition(x,y);
            }

            tileGrid.SetTileValue(x, y, number);
            int value = lowerNumber ? number - 1 : number;

            // Recur for north, east, south and west
            RoomCreateRecurse(grid, x + 1, y, room, firstNumber, lowerNumber, value);
            RoomCreateRecurse(grid, x - 1, y, room, firstNumber, lowerNumber, value);
            RoomCreateRecurse(grid, x, y + 1, room, firstNumber, lowerNumber, value);
            RoomCreateRecurse(grid, x, y - 1, room, firstNumber, lowerNumber, value);
        }

        public void RoomCreate(TileGrid grid, int x, int y, Room room, bool lowerNumber = false, int number = -1)
        {
            grid.ClearNumbers();
            RoomFillHelper(grid, x, y, room, true, lowerNumber, number);
        }

        public void Initialize()
        {
            CreateRooms();
        }

        public void Fill(int2 pos)
        {
            if (config.Occupance == OccupanceType.Contains_A)
            {
                tileGrid.SetTileId(pos, config.TileA);
            }
            else
            {
                tileGrid.SetTileId(pos, config.FillTile);
            }
        }
    }
}