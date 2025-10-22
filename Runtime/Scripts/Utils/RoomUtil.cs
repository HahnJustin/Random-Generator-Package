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

        public bool DoInitialization { get; set; }

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
            DoInitialization = true;
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
            foreach (int2 pos in tileGrid.GetPositions()) 
            { 
                if (tileGrid.IsInRegion(pos) && IsOccupied(pos) <= 0 && tileGrid.GetTileValue(pos) == 0)
                {
                    Room newRoom = new(currentRoomNumber);
                    RoomFill(tileGrid, pos.x, pos.y, newRoom, true);
                    rooms.Add(newRoom.Value, newRoom);
                    currentRoomNumber -= 1;
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

        private void RoomFillHelper(
            TileGrid grid,
            int x,
            int y,
            Room room,
            bool useNumbers,
            bool decreaseNumbers = false,
            int number = -1)
        {
            Queue<Tuple<int2, int>> queue = new();
            queue.Enqueue(new(new int2(x, y), number));

            // Local helper that only enqueues truly "open & unvisited" cells
            bool TryEnqueueIfOpen(int nx, int ny, int nnum)
            {
                // Out of bounds -> treat as blocked for edge detection
                if (!grid.IsInBounds(nx, ny))
                    return false;

                // Outside region or occupied -> blocked
                if (!grid.IsInRegion(nx, ny) || IsOccupied(nx, ny) >= 1)
                    return false;

                // When numbering, only expand into *unnumbered* (value == 0) cells
                if (useNumbers && grid.GetTileValue(nx, ny) < 0)
                    return false;

                queue.Enqueue(new(new int2(nx, ny), nnum));
                return true;
            }

            while (queue.Count > 0)
            {
                var tuple = queue.Dequeue();
                int2 point = tuple.Item1;
                int currentNum = tuple.Item2;

                // Skip if not usable now (already numbered/visited or blocked)
                if (!grid.IsInRegion(point) ||
                    IsOccupied(point) >= 1 ||
                    (useNumbers && grid.GetTileValue(point) < 0))
                {
                    continue;
                }

                // Record membership
                room?.AddPosition(point);

                // Mark visited
                if (decreaseNumbers && useNumbers) tileGrid.SetTileValue(point, currentNum);
                else if (useNumbers) tileGrid.SetTileValue(point, currentRoomNumber);
                else Fill(point);

                if (decreaseNumbers) currentNum -= 1;

                // Enqueue neighbors only if open & unvisited; track if any sides are blocked for edge marking
                bool leftOpen = TryEnqueueIfOpen(point.x - 1, point.y, currentNum);
                bool rightOpen = TryEnqueueIfOpen(point.x + 1, point.y, currentNum);
                bool downOpen = TryEnqueueIfOpen(point.x, point.y - 1, currentNum);
                bool upOpen = TryEnqueueIfOpen(point.x, point.y + 1, currentNum);

                // If any side is not open, itfs an edge
                if (room != null && (!leftOpen || !rightOpen || !downOpen || !upOpen))
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