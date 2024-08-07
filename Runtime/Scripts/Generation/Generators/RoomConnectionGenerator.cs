using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomConnectionGenerator : RoomGenerator
    {
        protected new RoomConnectionConfig config;

        private readonly int INITIAL_RING_VALUE = 1;

        private record RoomInfo(Room Room, Tile Tile, int ringCount);

        private List<Room> currentRooms;

        public RoomConnectionGenerator(RoomConnectionConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            currentRooms = LargestFirstRoomList;
            List<Direction> directions = new() { Direction.Down, Direction.Up, Direction.Right, Direction.Left };

            for (int i = currentRooms.Count - 1; i >= 0; i--)
            {
                Room room = currentRooms[i];
                if (room == null)
                {
                    continue;
                }

                TileGrid.ClearPositiveNumbers();
                List<Tile> ring = room.GetEdges();
                Dictionary<int, RoomInfo> roomInfos = new();
                int ringValue = INITIAL_RING_VALUE;

                while (((roomInfos.Count <= 0 && !config.AdditionalConnections) ||
                       ((roomInfos.Count <= 0 || ringValue < config.AdditionalUpperBound + INITIAL_RING_VALUE) && config.AdditionalConnections)) &&
                       ring.Count > 0)
                {
                    List<Tile> tempRing = new();

                    foreach (Tile tile in ring)
                    {
                        directions.Shuffle(random);
                        foreach (Direction direction in directions)
                        {
                            Vector2Int point = GetPointInDirection(tile.Vector, direction);
                            Tile adjacentTile = TileGrid.GetTile(point);

                            if (adjacentTile == null) continue;

                            int adjValue = adjacentTile.Value;
                            if (adjValue == 0)
                            {
                                adjacentTile.Value = ringValue;
                                tempRing.Add(adjacentTile);
                            }
                            else if (adjValue < 0 && adjValue != room.Value && !roomInfos.ContainsKey(adjValue) &&
                                (!config.AdditionalConnections || (ringValue >= config.AdditionalLowerBound || roomInfos.Count == 0)))
                            {
                                roomInfos.Add(adjacentTile.Value, new(room, adjacentTile, ringValue));
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
        }

        private void CreatePathBetweenRooms(RoomInfo info, bool consolidateRooms)
        {
            List<Tile> path = new();
            List<Direction> directions = new() { Direction.Down, Direction.Up, Direction.Right, Direction.Left };

            Room room = info.Room;
            Tile tile = info.Tile;
            int value = info.ringCount;

            while(value >= INITIAL_RING_VALUE)
            {
                value -= 1;
                directions.Shuffle(random);
                foreach (Direction direction in directions)
                {
                    Vector2Int point = GetPointInDirection(tile.Vector, direction);
                    Tile adjacentTile = TileGrid.GetTile(point);
                    if (adjacentTile == null) continue;
                    else if (adjacentTile.Value == value)
                    {
                        path.Add(adjacentTile);
                        tile = adjacentTile;
                        break;
                    }
                }
            }

            foreach (Tile pathTile in path)
            {
                TileGrid.SetTileType(pathTile.Vector, config.HallwayTile);
                if (consolidateRooms)
                {
                    room.AddEdge(pathTile);
                    pathTile.Value = room.Value;
                }
            }

            if (consolidateRooms) 
            {
                Room room2 = Rooms[info.Tile.Value];
                currentRooms.Remove(room2);
                Rooms.Remove(room2.Value);

                foreach (Tile roomTile in room2)
                {
                    roomTile.Value = room.Value;
                }

                room.AddEdgeRange(room2.GetEdges());
            }
        }
    }
}
