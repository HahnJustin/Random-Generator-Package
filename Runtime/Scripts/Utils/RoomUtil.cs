using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using System;

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

        private bool EnqueueIfMatches(TileGrid grid, Queue<Tuple<Vector2Int, int>> queue, int x, int y, int number)
        {
            // Outta bounds
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height)
            {
                return true;
            }

            Tile tile = grid.GetTile(x, y);

            // Isn't Occupied
            if (IsOccupied(tile) <= 0)
            {
                queue.Enqueue(new(new Vector2Int(x, y), number));
                return false;
            }
            return true;
        }

        protected void CreateRooms()
        {
            tileGrid.ClearNumbers();
            rooms = new();
            for (int j = 0; j < tileGrid.width; j++)
            {
                for (int k = 0; k < tileGrid.height; k++)
                {
                    Tile tile = tileGrid.GetTile(j, k);
                    if (IsOccupied(tileGrid.GetTile(j, k)) <= 0 && tile.Value == 0)
                    {
                        Room newRoom = new(currentRoomNumber);
                        RoomFill(tileGrid, j, k, newRoom, true);
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

        public void RoomFill(int x, int y, Room room, bool useNumbers)
        {
            RoomFillHelper( tileGrid, x, y, room, useNumbers);
        }

        public void RoomFill(TileGrid grid, int x, int y, Room room, bool useNumbers)
        {
            RoomFillHelper(grid, x, y, room, useNumbers);
        }

        private void RoomFillHelper(TileGrid grid, int x, int y, Room room, bool useNumbers, bool decreaseNumbers = false, int number = -1)
        {
            Queue<Tuple<Vector2Int,int>> queue = new Queue<Tuple<Vector2Int, int>>();
            queue.Enqueue(new(new Vector2Int(x, y), number));

            while (queue.Any())
            {
                Tuple<Vector2Int, int> tuple = queue.Dequeue();
                Vector2Int point = tuple.Item1;
                int currentNum = tuple.Item2;
                Tile tile = grid.GetTile(point);

                // Is Occupied
                if (IsOccupied(tile) >= 1 || (tile.Value < 0 && useNumbers))
                    continue;
                if (room != null)
                {
                    room.AddTile(tile);
                }

                if(decreaseNumbers && useNumbers) tile.Value = currentNum;
                else if (useNumbers) tile.Value = currentRoomNumber;
                else Fill(tile);

                if (decreaseNumbers) currentNum -= 1;

                bool leftOccupied = EnqueueIfMatches(grid, queue, point.x - 1, point.y, currentNum);
                bool rightOccupied = EnqueueIfMatches(grid, queue, point.x + 1, point.y, currentNum);
                bool downOccupied = EnqueueIfMatches(grid, queue, point.x, point.y - 1, currentNum);
                bool upOccupied = EnqueueIfMatches(grid, queue, point.x, point.y + 1, currentNum);

                if ((leftOccupied || rightOccupied || downOccupied || upOccupied) && room != null)
                {
                    room.AddEdge(tile);
                }
            }
        }

        private void RoomCreateRecurse(TileGrid grid, int x, int y, Room room, int firstNumber, bool lowerNumber = false, int number = -1)
        {
            // Base cases
            if (x < 0 || x >= grid.width ||
                y < 0 || y >= grid.height)
                return;

            Tile tile = grid.GetTile(x, y);

            // Is occupied
            if (IsOccupied(tile) >= 1 || tile.Value <= firstNumber)
                return;

            //Add
            if (room != null)
            {
                room.AddTile(tile);
            }

            tile.Value = number;
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
    }
}