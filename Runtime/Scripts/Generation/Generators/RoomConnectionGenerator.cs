using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Utils;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomConnectionGenerator : AbstractGenerator<RoomConnectionConfig>
    {
        private RoomUtil util;
        private readonly int INITIAL_RING_VALUE = 1;

        private class RoomInfo
        {
            public Room Room { get; }
            public int2 Position { get; }
            public int RingCount { get; }

            public RoomInfo(Room room, int2 pos, int ringCount)
            {
                Room = room;
                Position = pos;
                RingCount = ringCount;
            }
        }

        private List<Room> currentRooms;

        public RoomConnectionGenerator(RoomConnectionConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            currentRooms = util.LargestFirstRoomList;
            List<Direction> directions = new() { Direction.Down, Direction.Up, Direction.Right, Direction.Left };

            for (int i = currentRooms.Count - 1; i >= 0; i--)
            {
                Room room = currentRooms[i];
                if (room == null)
                {
                    continue;
                }

                TileGrid.ClearPositiveNumbers();
                List<int2> ring = room.EdgeList;
                Dictionary<int, RoomInfo> roomInfos = new();
                int ringValue = INITIAL_RING_VALUE;

                while (((roomInfos.Count <= 0 && !config.AdditionalConnections) ||
                       ((roomInfos.Count <= 0 || ringValue < config.AdditionalUpperBound + INITIAL_RING_VALUE) && config.AdditionalConnections)) &&
                       ring.Count > 0)
                {
                    List<int2> tempRing = new();

                    foreach (int2 pos in ring)
                    {
                        directions.Shuffle(random);
                        foreach (Direction direction in directions)
                        {
                            int2 point = pos.GetPointInDirection(direction);

                            if (!TileGrid.IsRestricted(point)) continue;

                            int adjValue = TileGrid.GetTileValue(point);
                            if (adjValue == 0)
                            {
                                TileGrid.SetTileValue(point, ringValue);
                                tempRing.Add(point);
                            }
                            else if (adjValue < 0 && adjValue != room.Value && !roomInfos.ContainsKey(adjValue) &&
                                (!config.AdditionalConnections || (ringValue >= config.AdditionalLowerBound || roomInfos.Count == 0)))
                            {
                                roomInfos.Add(adjValue, new(room, point, ringValue));
                                if (!config.AdditionalConnections) break;
                            }
                        }
                        if(!config.AdditionalConnections && roomInfos.Count > 0) break;
                    }
                    ring = tempRing;
                    ringValue += 1;
                    CancelCheck();
                }

                bool consolidate = true;
                //Creating the paths between the points
                foreach (KeyValuePair<int, RoomInfo> pair in roomInfos)
                {
                    CreatePathBetweenRooms(pair.Value, consolidate);
                    consolidate = false;
                }
            }
            TileGrid.ClearPositiveNumbers();
            return input;
        }

        private void CreatePathBetweenRooms(RoomInfo info, bool consolidateRooms)
        {
            List<int2> path = new();
            List<Direction> directions = new() { Direction.Down, Direction.Up, Direction.Right, Direction.Left };

            Room room = info.Room;
            int2 pos = info.Position;
            int value = info.RingCount;

            while(value >= INITIAL_RING_VALUE)
            {
                value -= 1;
                directions.Shuffle(random);
                foreach (Direction direction in directions)
                {
                    int2 curr = pos.GetPointInDirection(direction);
                    if (!TileGrid.IsRestricted(curr)) continue;
                    else if (TileGrid.GetTileValue(curr) == value)
                    {
                        path.Add(curr);
                        pos = curr;
                        break;
                    }
                }
            }

            foreach (int2 pathPos in path)
            {
                TileGrid.SetTileId(pathPos, config.HallwayTile);
                if (consolidateRooms)
                {
                    room.AddEdge(pathPos);
                    TileGrid.SetTileValue(pathPos, room.Value);
                }
            }

            if (consolidateRooms) 
            {
                Room room2 = util.Rooms[TileGrid.GetTileValue(pos)];
                currentRooms.Remove(room2);
                util.Rooms.Remove(room2.Value);

                foreach (int2 roomPos in room2)
                {
                    TileGrid.SetTileValue(roomPos, room.Value);
                }

                room.AddEdgeRange(room2.EdgeList);
            }
        }
    }
}