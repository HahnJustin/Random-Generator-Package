using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using System.Linq;
using Dalichrome.RandomGenerator.Random;
using System;

namespace Dalichrome.RandomGenerator.Generators
{

    public class NystromDungeonGenerator: RoomGenerator
    {
        protected new NystromDungeonConfig config;
        private readonly MazeUtil util;

        private Dictionary<int,DungeonRoom> dungeonRoomDict;
        private List<Vector2Int> connectors;

        private readonly int ORIGINAL_VALUE = 4;
        private readonly int MAZE_WALL_VALUE = 3;
        private readonly int WALL_VALUE = 1;

        private readonly int ROOM_VALUE = -1;
        private readonly int DOOR_VALUE = -2;
        private readonly int MAZE_FLOOR_VALUE = -3;

        private readonly int ROOM_WIDTH_MIN = 3;
        private readonly int ROOM_HEIGHT_MIN = 3;

        private List<Direction> cardinalDirections = new()
        {
            Direction.Right,
            Direction.Down,
            Direction.Left,
            Direction.Up
        };

        private List<Direction> eightDirections = new()
        {
            Direction.Right,
            Direction.Down_Right,
            Direction.Down,
            Direction.Down_Left,
            Direction.Left,
            Direction.Up_Left,
            Direction.Up,
            Direction.Up_Right
        };

        private class DungeonRoom
        {
            public int Id { get { return id; } }
            private int id;

            public List<Vector2Int> Walls { get { return walls; } }
            private List<Vector2Int> walls = new();

            public List<Vector2Int> Connectors { get { return connectors; } }
            private List<Vector2Int> connectors = new();

            public DungeonRoom(int id)
            {
                this.id = id;
            }

            public List<Vector2Int> Join(DungeonRoom room)
            {
                List<Vector2Int> culled = new();
                Walls.AddRange(room.Walls);

                for (int i = Connectors.Count - 1; i >= 0; i--)
                {
                    if (room.Connectors.Contains(Connectors[i]))
                    {
                        culled.Add(Connectors[i]);
                        room.Connectors.Remove(Connectors[i]);
                        Connectors.RemoveAt(i);
                    }
                }
                Connectors.AddRange(room.Connectors);
                return culled;
            }
        }

        public NystromDungeonGenerator(NystromDungeonConfig config) : base(config)
        {
            this.config = config;

            util = new(config);
            util.OutOfBoundsOccupancy = 1;
            AddUtil(util);

        }

        private void EnqueueIfMatches(int[,] grid, Queue<Vector2Int> queue, int x, int y, int fillable)
        {
            // Outta bounds
            if (!grid.InBounds(x, y)) return;

            int value = grid[x, y];

            // Isn't Occupied
            if (value == fillable)
            {
                queue.Enqueue(new Vector2Int(x, y));
            }
        }

        private void MazeFill(int[,] grid, int x, int y, DungeonRoom room, int filled, int fillable, int wall)
        {
            Queue<Vector2Int> queue = new();
            queue.Enqueue(new Vector2Int(x, y));

            while (queue.Any())
            {
                Vector2Int point = queue.Dequeue();
                int value = grid[point.x, point.y];

                // Is Occupied
                if (value != fillable) continue;

                grid[point.x, point.y] = filled;

                //grid.SetNeighbors(x,y, neighborFunc);
                foreach (Direction direction in eightDirections)
                {
                    Vector2Int nextTo = GetPointInDirection(point, direction);
                    if (!grid.InBounds(nextTo)) continue;

                    if (grid[nextTo.x, nextTo.y] == MAZE_WALL_VALUE)
                    {
                        grid[nextTo.x, nextTo.y] = wall;
                    }
                    room.Walls.Add(nextTo);
                }

                EnqueueIfMatches(grid, queue, point.x - 1, point.y, fillable);
                EnqueueIfMatches(grid, queue, point.x + 1, point.y, fillable);
                EnqueueIfMatches(grid, queue, point.x, point.y - 1, fillable);
                EnqueueIfMatches(grid, queue, point.x, point.y + 1, fillable);
            }
        }

        protected override void Enact()
        {
            int[,] occupanceGrid = util.GetOccupanceGrid();

            foreach (Room room in RoomList)
            {
                BoundsInt bounds = room.GetBounds();
                if (room.Height <= Mathf.Max(ROOM_HEIGHT_MIN, config.RoomMinSize) &&
                    room.Width <= Mathf.Max(ROOM_WIDTH_MIN, config.RoomMinSize)) continue;


                //Fill room being worked on to ROOM_VALUE (Should be negative)
                foreach (Tile tile in room)
                {
                    occupanceGrid[tile.x, tile.y] = ROOM_VALUE;
                }

                int value = ORIGINAL_VALUE;
                dungeonRoomDict = new();
                connectors = new();

                //Brutal Dungeon Room Attempt Loop
                for (int i = 0; i < config.RoomPlaceIterations; i++)
                {
                    int dunRoomWidth = Mathf.Clamp(random.NextInt(config.RoomMinSize, config.RoomMaxSize), ROOM_WIDTH_MIN, room.Width - 1);
                    int dunRoomHeight = Mathf.Clamp(random.NextInt(config.RoomMinSize, config.RoomMaxSize), ROOM_HEIGHT_MIN, room.Height -1);

                    if (dunRoomWidth % 2 == 0) dunRoomWidth += 1;
                    if (dunRoomHeight % 2 == 0) dunRoomHeight += 1;

                    int initialX = random.NextInt(bounds.x, bounds.xMax - dunRoomWidth);
                    int initialY = random.NextInt(bounds.y, bounds.yMax - dunRoomHeight);

                    if (initialX % 2 == 0) initialX += 1;
                    if (initialY % 2 == 0) initialY += 1;

                    bool breakIt = false;

                    //Dungeon Room Creation Attempt
                    for(int x = initialX; x < initialX + dunRoomWidth; x++)
                    {
                        if (breakIt) break;
                        for (int y = initialY; y < initialY + dunRoomHeight; y++)
                        {
                            if (!occupanceGrid.InBounds(x, y) || occupanceGrid[x, y] != ROOM_VALUE)
                            {
                                breakIt = true;
                                break;
                            }
                        }
                    }
                    CancelCheck();
                    if (breakIt) continue;

                    DungeonRoom dunRoom = new(value);
                    dungeonRoomDict.Add(value, dunRoom);

                    //Apply Dungeon Rooms to OccupanceGrid
                    for (int x = initialX - 1; x < initialX + dunRoomWidth + 1; x++)
                    {
                        for (int y = initialY - 1; y < initialY + dunRoomHeight + 1; y++)
                        {
                            if (!occupanceGrid.InBounds(x, y)) continue;
                            int gridValue = occupanceGrid[x, y];

                            //Wall
                            if (x == initialX - 1 || y == initialY - 1 || x == initialX + dunRoomWidth || y == initialY + dunRoomHeight)
                            {
                                occupanceGrid[x, y] = value;
                                dunRoom.Walls.Add(new Vector2Int(x, y));
                            }
                            //Floor
                            else if (gridValue == ROOM_VALUE)
                            {
                                occupanceGrid[x, y] = -value;
                            }
                        }
                    }
                    value += 1;
                }

                //Set Maze variables for use in util
                util.FirstFloorValue = MAZE_FLOOR_VALUE;
                util.FirstWallValue = WALL_VALUE;

                util.FirstMazeFloorValue = MAZE_FLOOR_VALUE;
                util.FirstMazeWallValue = MAZE_WALL_VALUE;

                //Create Maze in between rooms in occupance
                util.CreateMazeInRoomGrid(occupanceGrid, room, random);

                //Change maze variables as they will be flood filled and changed next step
                util.FirstMazeFloorValue = -value;
                util.FirstMazeWallValue = value;

                //Flood Fill Maze to turn unconnected portions into different 'dungeon-rooms'
                for (int x = 0; x < occupanceGrid.GetLength(0); x++)
                {
                    for (int y = 0; y < occupanceGrid.GetLength(1); y++)
                    {
                        if (occupanceGrid[x,y] == MAZE_FLOOR_VALUE)
                        {
                            DungeonRoom newRoom = new(value);
                            MazeFill(occupanceGrid, x,y, newRoom, -value, MAZE_FLOOR_VALUE, value);
                            dungeonRoomDict.Add(value, newRoom);
                            value += 1;
                        }
                    }
                }

                //Create Connectors
                foreach (KeyValuePair<int, DungeonRoom> pair in dungeonRoomDict) 
                {
                    DungeonRoom dunRoom = pair.Value;
                    foreach (Vector2Int wallTile in dunRoom.Walls)
                    {
                        int amount = 0;
                        KeyValuePair<int, int> ids = new(0,0);
                        foreach (Direction direction in cardinalDirections)
                        {
                            Vector2Int neighbor = GetPointInDirection(wallTile, direction);
                            if (!occupanceGrid.InBounds(neighbor)) continue;

                            int neighborValue = occupanceGrid[neighbor.x, neighbor.y];
                            if (neighborValue <= MAZE_FLOOR_VALUE)
                            {
                                amount += 1;
                                if (ids.Key == 0) ids = new(neighborValue, 0);
                                else ids = new(ids.Key, neighborValue);
                            }
                        }
                        if (amount == 2 && ids.Key != ids.Value) 
                        { 
                            dunRoom.Connectors.Add(wallTile);
                            if(!connectors.Contains(wallTile)) connectors.Add(wallTile);
                        }
                    }
                    CancelCheck();
                }
                
                //Create Connections
                List<DungeonRoom> dungeonRooms = dungeonRoomDict.Values.ToList();

                if (dungeonRooms.Count == 0) continue;

                DungeonRoom mainRoom = dungeonRooms[random.NextInt(dungeonRooms.Count)];
                List<int> mergedIds = new List<int> { mainRoom.Id };
                while (dungeonRooms.Count > 1 && mainRoom.Connectors.Count > 0)
                {
                    int index = random.NextInt(mainRoom.Connectors.Count);
                    Vector2Int connector = mainRoom.Connectors[index];
                    cardinalDirections.Shuffle(random);

                    foreach (Direction direction in cardinalDirections)
                    {
                        Vector2Int neighbor = GetPointInDirection(connector, direction);
                        if (!occupanceGrid.InBounds(neighbor)) continue;

                        int connectingId = occupanceGrid[neighbor.x, neighbor.y];
                        if (!mergedIds.Contains(-connectingId) && dungeonRoomDict.ContainsKey(-connectingId) && connectingId <= MAZE_FLOOR_VALUE)
                        {
                            mergedIds.Add(-connectingId);
                            occupanceGrid[connector.x, connector.y] = DOOR_VALUE;

                            DungeonRoom connecting = dungeonRoomDict[-connectingId];
                            List<Vector2Int> culled = mainRoom.Join(connecting);
                            if (culled.Count != 0 && random.NextFloat() <= config.ExtraDoorOdds)
                            {
                                Vector2Int chanceDoor = culled[random.NextInt(culled.Count)];
                                occupanceGrid[chanceDoor.x, chanceDoor.y] = DOOR_VALUE;                       
                            }
                            connectors.RemoveAll( x => culled.Contains(x));
                            dungeonRooms.Remove(connecting);
                        }
                    }
                    mainRoom.Connectors.Remove(connector);
                    connectors.Remove(connector);
                    CancelCheck();
                }

                Debug.Log("Connectors Left:" + connectors.Count);
                foreach (Vector2Int position in connectors)
                {
                    TileGrid.SetTileType(position, TileType.Debug_Circle_Red);
                }

                //must redefine pruning to consider all values of 'maze' rooms
                if (config.PruneDeadends) util.RemoveDeadEnds(occupanceGrid);
                
                //Apply Dungeon Rooms and Corridors to Tilegrid
                for (int x = 0; x < occupanceGrid.GetLength(0); x++)
                {
                    for (int y = 0; y < occupanceGrid.GetLength(1); y++)
                    {
                        int gridValue = occupanceGrid[x, y];
                        if (gridValue == ROOM_VALUE || gridValue == 0 || gridValue == 1) continue;

                        //Room Floor 
                        if (gridValue <= MAZE_FLOOR_VALUE)
                        {
                            TileGrid.SetTileType(x, y, config.HallwayTile);
                        }
                        //Room Wall
                        else if(gridValue >= MAZE_WALL_VALUE || gridValue == DOOR_VALUE)
                        {
                            TileGrid.SetTileType(x, y, config.WallTile);
                            if(gridValue == DOOR_VALUE) TileGrid.SetTileType(x, y, config.DoorTile);
                        }
                        TileGrid.SetTileType(x, y, config.FloorTile);
                        TileGrid.SetTileValue(x,y, gridValue);
                    }
                }
            }
        }
    }
}
