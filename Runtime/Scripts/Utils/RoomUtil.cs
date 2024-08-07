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

        private bool EnqueueIfMatches(TileGrid grid, Queue<Vector2Int> queue, int x, int y)
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
                queue.Enqueue(new Vector2Int(x, y));
                return false;
            }
            return true;
        }

        private void RoomFillUtilRecurse(TileGrid grid, int x, int y, Room room)
        {
            // Base cases
            if (x < 0 || x >= grid.width ||
                y < 0 || y >= grid.height)
                return;

            Tile tile = grid.GetTile(x, y);

            // Is occupied
            if (IsOccupied(tile) >= 1)
                return;

            // Fill
            if (room != null)
            {
                room.AddTile(tile);
            }
            Fill(tile);
            tile.Value = 1;

            // Recur for north, east, south and west
            RoomFillUtilRecurse(grid, x + 1, y, room);
            RoomFillUtilRecurse(grid, x - 1, y, room);
            RoomFillUtilRecurse(grid, x, y + 1, room);
            RoomFillUtilRecurse(grid, x, y - 1, room);
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
            RoomFill( tileGrid, x, y, room, useNumbers);
        }

        public void RoomFill(TileGrid grid, int x, int y, Room room, bool useNumbers)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));

            while (queue.Any())
            {
                Vector2Int point = queue.Dequeue();
                Tile tile = grid.GetTile(point);

                // Is Occupied
                if (IsOccupied(tile) >= 1 || (tile.Value < 0 && useNumbers))
                    continue;
                if (room != null)
                {
                    room.AddTile(tile);
                }

                if (useNumbers) tile.Value = currentRoomNumber;
                else Fill(tile);

                bool leftOccupied = EnqueueIfMatches(grid, queue, point.x - 1, point.y);
                bool rightOccupied = EnqueueIfMatches(grid, queue, point.x + 1, point.y);
                bool downOccupied = EnqueueIfMatches(grid, queue, point.x, point.y - 1);
                bool upOccupied = EnqueueIfMatches(grid, queue, point.x, point.y + 1);

                if ((leftOccupied || rightOccupied || downOccupied || upOccupied) && room != null)
                {
                    room.AddEdge(tile);
                }
            }
        }

        public void Initialize()
        {
            CreateRooms();
        }
    }
}