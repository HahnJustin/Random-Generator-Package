using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public class MazeUtil : RoomUtil
    {
        protected new IMazeConfig config;

        private MazeCell[,] cellGrid;

        private List<MazeCell> cells;
        
        public int RoomValue { get { return _roomValue; } set { _roomValue = value; } }
        private int _roomValue = -1;

        public int FirstWallValue { get { return _wallValue; } set { _wallValue = value; } }
        private int _wallValue = 1;

        public int FirstFloorValue { get { return _floorValue; } set { _floorValue = value; } }
        private int _floorValue = 0;

        public int FirstMazeWallValue { get { return _mazeWallValue; } set { _mazeWallValue = value; }  }
        private int _mazeWallValue = 4;

        public int FirstMazeFloorValue { get { return _mazeFloorValue; } set { _mazeFloorValue = value; } }
        private int _mazeFloorValue = 5;

        private static readonly Direction[] CARDINAL = new[]
        {
            Direction.Right, Direction.Down, Direction.Left, Direction.Up
        };

        private class MazeCell
        {
            public int X { get { return x; } }
            private int x = 0;

            public int Y { get { return y; } }
            private int y = 0;

            public bool visited = false;

            public MazeCell(int2 position)
            {
                x = position.x;
                y = position.y;
                visited = false;
            }
        }

        public MazeUtil(IMazeConfig config) : base(config)
        {
            this.config = config;
        }

        private MazeCell CreateMazeCell(int2 pos)
        {
            MazeCell maze = new(pos);
            cellGrid[pos.x, pos.y] = maze;
            cells.Add(maze);
            return maze;
        }

        private void VisitMazeCell(MazeCell maze)
        {
            maze.visited = true;
            cells.Remove(maze);
        }

        private MazeCell GetRandomCellNextToPosition(AbstractRandom random, int x, int y)
        {
            // make a local copy and shuffle it; do NOT touch the canonical array
            Direction[] dirs = (Direction[])CARDINAL.Clone();
            dirs.Shuffle(random);  // your Fisher–Yates over arrays

            var position = new Vector2Int(x, y);
            foreach (var direction in dirs)
            {
                var DirectedPoint = position.GetPointInDirection(direction, 2);

                if (!tileGrid.IsInBounds(DirectedPoint.x, DirectedPoint.y)) continue;

                if (IsOccupied(DirectedPoint) == 0 &&
                    cellGrid[DirectedPoint.x, DirectedPoint.y] != null &&
                    !cellGrid[DirectedPoint.x, DirectedPoint.y].visited)
                {
                    return cellGrid[DirectedPoint.x, DirectedPoint.y];
                }
            }
            return null;
        }

        //Converts Technical Tile in Wall and Technical Tile 2 into Carve
        private void CovertTechnicalTiles(TileGrid tileGrid)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tileGrid.ColumnContainsId(x, y, (int)TileDefaults.Debug_Technical))
                    {
                        tileGrid.SetTileId(x, y , (int)TileDefaults.Debug_NA);
                        tileGrid.SetTileId(x, y, (int)config.WallTile);
                    }
                    else if (tileGrid.ColumnContainsId(x, y, (int)TileDefaults.Debug_Technical2))
                    {
                        tileGrid.SetTileId(x, y, (int)TileDefaults.Debug_NA);
                        tileGrid.SetTileId(x, y, (int)config.HallwayTile);
                    }
                }
            }
        }

        //Optimization: Can fix that entire readGrid is iterated through per room if could find bounds of room
        private void InitializeMazeCellsInRoom(Room room)
        {
            cellGrid = new MazeCell[width, height];
            cells = new();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!room.ContainsPosition(x,y)) continue;

                    if (x % 2 == 1 && y % 2 == 1)
                    {
                        cellGrid[x, y] = CreateMazeCell(new int2(x,y));
                        tileGrid.SetTileId(x, y, (int) TileDefaults.Debug_Technical2);
                    }
                    else
                    {
                        tileGrid.SetTileId(x, y, (int) TileDefaults.Debug_Technical);
                    }
                }
            }
        }

        private void InitializeMazeCellsInRoomGrid(int[,] grid, BoundsInt bounds)
        {
            // Use the actual grid size for safety (not tileGrid.width/height),
            // and clamp the bounds to the grid extents.
            int gx = grid.GetLength(0);
            int gy = grid.GetLength(1);

            cellGrid = new MazeCell[gx, gy];
            cells = new();

            int x0 = math.max(bounds.x, 0);
            int x1 = math.min(bounds.xMax, gx);  // BoundsInt.xMax is exclusive
            int y0 = math.max(bounds.y, 0);
            int y1 = math.min(bounds.yMax, gy);  // BoundsInt.yMax is exclusive

            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    // Only touch cells that belong to this room
                    if (grid[x, y] != RoomValue) continue;

                    if ((x & 1) == 1 && (y & 1) == 1)
                    {
                        cellGrid[x, y] = CreateMazeCell(new int2(x, y));
                        grid[x, y] = FirstMazeFloorValue;   // e.g., -3 for Nystrom
                    }
                    else
                    {
                        grid[x, y] = FirstMazeWallValue;    // e.g.,  3 for Nystrom
                    }
                }
            }
        }

        private Vector2Int? GetParentPathIfDeadEnd(int[,] grid, Vector2Int? pos)
        {
            if (pos == null) return null;

            Vector2Int floor = default;
            int wallAmount = 0;
            foreach (var direction in CARDINAL)
            {
                Vector2Int neighbor = ((Vector2Int)pos).GetPointInDirection(direction);
                if (!grid.InBounds(neighbor)) 
                {
                    wallAmount += 1;
                    continue;
                }

                int value = grid[neighbor.x, neighbor.y];
                if (value == RoomValue || value >= FirstWallValue)
                {
                    wallAmount += 1;
                }
                else floor = neighbor;
            }

            if (wallAmount == 3) return floor;
            else if (wallAmount == 4) return pos;
            return null;
        }

        public void CreateMazeInRoomGrid(int[,] grid, Room room, AbstractRandom random)
        {
            cells = new();
            InitializeMazeCellsInRoomGrid(grid, room.Bounds);

            if (cells.Count <= 0) return;

            Queue<MazeCell> unvisitedCellQueue = new();
            MazeCell currentCell = cells[random.NextInt(0, cells.Count)];

            while (true)
            {
                unvisitedCellQueue.Enqueue(currentCell);
                VisitMazeCell(currentCell);

                while (true)
                {
                    MazeCell neighborCell = GetRandomCellNextToPosition(random, currentCell.X, currentCell.Y);
                    if (neighborCell == null && unvisitedCellQueue.Count > 0)
                    {
                        currentCell = unvisitedCellQueue.Dequeue();
                        continue;
                    }
                    else if (neighborCell == null)
                    {
                        break;
                    }

                    grid[(currentCell.X + neighborCell.X) / 2, (currentCell.Y + neighborCell.Y) / 2] = FirstMazeFloorValue;
                    currentCell = neighborCell;
                    break;
                }

                if (unvisitedCellQueue.Count != 0) continue;
                else if (cells.Count == 0) break;
                else if (cells.Count > 0) currentCell = cells[random.NextInt(0, cells.Count)];
            }
        }

        public void RemoveDeadEnds(int[,] grid)
        {
            //Removes Dead End Floors
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] > FirstMazeFloorValue) continue;
                    Vector2Int? pos = new(x, y);

                    while (pos != null)
                    {
                        Vector2Int? next = GetParentPathIfDeadEnd(grid, pos);

                        if (next != null && pos is Vector2Int floor)
                        {
                            grid[floor.x, floor.y] = RoomValue;
                        }
                        if (pos == next) break;
                        pos = next;
                    }
                }
            }

            //Removes Walls
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] < FirstMazeWallValue) continue;
                    Vector2Int pos = new(x, y);

                    if(!grid.IfNeighborDoes(x, y, x=> x <= FirstMazeFloorValue))
                    {
                        grid[x, y] = RoomValue;
                    }                   
                }
            }

            //Adds Walls
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] <= FirstMazeFloorValue)
                    {
                        Vector2Int pos = new(x, y);
                        grid.SetNeighbors(x,y, x => x == RoomValue || x == FirstWallValue ? FirstMazeWallValue : x);
                    }
                }
            }
        }

        public void CreateMazeInRoom(Room room, AbstractRandom random)
        {
            cells = new();
            InitializeMazeCellsInRoom(room);

            if (cells.Count <= 0) return;

            Queue<MazeCell> unvisitedCellQueue = new();
            MazeCell currentCell = cells[random.NextInt(0, cells.Count)];

            while (true)
            {
                unvisitedCellQueue.Enqueue(currentCell);
                VisitMazeCell(currentCell);

                while (true)
                {
                    MazeCell neighborCell = GetRandomCellNextToPosition(random, currentCell.X, currentCell.Y);
                    if (neighborCell == null && unvisitedCellQueue.Count > 0)
                    {
                        currentCell = unvisitedCellQueue.Dequeue();
                        continue;
                    }
                    else if (neighborCell == null)
                    {
                        break;
                    }

                    tileGrid.SetTileId((currentCell.X + neighborCell.X) / 2,
                                          (currentCell.Y + neighborCell.Y) / 2,
                                          (int)TileDefaults.Debug_Technical2);
                    currentCell = neighborCell;
                    break;
                }

                //If the queue is empty, but cells is not, there is a part of the room that is 'contiguous' tile-wise
                //But cannot be contiguous labyrinth-wise, this code still lets those other parts be 'labyrinth'ed
                if (unvisitedCellQueue.Count != 0) continue;
                else if (cells.Count == 0) break;
                else if (cells.Count > 0) currentCell = cells[random.NextInt(0, cells.Count)];
            }

            CovertTechnicalTiles(tileGrid);
        }
    }
}