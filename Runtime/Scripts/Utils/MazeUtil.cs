using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;

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

        private List<Direction> allDirections = new()
        {
            Direction.Right,
            Direction.Down,
            Direction.Left,
            Direction.Up
        };

        private class MazeCell
        {
            public Tile tile;

            public int X { get { return x; } }
            private int x = 0;

            public int Y { get { return y; } }
            private int y = 0;

            public bool visited = false;

            public MazeCell(Tile tile)
            {
                this.tile = tile;
                visited = false;
            }

            public MazeCell(Vector2Int position)
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

        private MazeCell CreateMazeCell(Tile tile)
        {
            MazeCell maze = new(tile);
            cellGrid[tile.x, tile.y] = maze;
            cells.Add(maze);
            return maze;
        }

        private MazeCell CreateMazeCell(Vector2Int position)
        {
            MazeCell maze = new(position);
            cellGrid[position.x, position.y] = maze;
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
            allDirections.Shuffle(random);
            Vector2Int position = new(x, y);
            foreach (Direction direction in allDirections)
            {
                Vector2Int DirectedPoint = GetPointInDirection(position, direction, 2);

                //Is occupied works here only due to use of debug technical tiles
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
        private void CovertTechnicalTiles()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = tileGrid.GetTile(x, y);
                    if (tile.ContainsType(TileType.Debug_Technical))
                    {
                        tile.SetType(TileType.Debug_NA);
                        tile.SetType(config.WallTile);
                    }
                    else if (tile.ContainsType(TileType.Debug_Technical2))
                    {
                        tile.SetType(TileType.Debug_NA);
                        tile.SetType(config.HallwayTile);
                    }
                }
            }
        }

        //Optimization: Can fix that entire grid is iterated through per room if could find bounds of room
        private void InitializeMazeCellsInRoom(Room room)
        {
            cellGrid = new MazeCell[width, height];
            cells = new();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = tileGrid.GetTile(x, y);
                    if (!room.ContainsTile(tile)) continue;

                    if (x % 2 == 1 && y % 2 == 1)
                    {
                        cellGrid[x, y] = CreateMazeCell(tile);
                        tile.SetType(TileType.Debug_Technical2);
                    }
                    else
                    {
                        tile.SetType(TileType.Debug_Technical);
                    }
                }
            }
        }

        private void InitializeMazeCellsInRoomGrid(int[,] grid, BoundsInt bounds)
        {
            cellGrid = new MazeCell[width, height];
            cells = new();
            foreach (Vector2Int pos in bounds.allPositionsWithin)
            {   
                if (grid[pos.x, pos.y] != RoomValue ) continue;

                if (pos.x % 2 == 1 && pos.y % 2 == 1)
                {
                    cellGrid[pos.x, pos.y] = CreateMazeCell(pos);
                    grid[pos.x, pos.y] = FirstMazeFloorValue;
                }
                else
                {
                    grid[pos.x, pos.y] = FirstMazeWallValue;
                }
            }
        }

        private Vector2Int? GetParentPathIfDeadEnd(int[,] grid, Vector2Int? pos)
        {
            if (pos == null) return null;

            Vector2Int floor = default;
            int wallAmount = 0;
            foreach (Direction direction in allDirections)
            {
                Vector2Int neighbor = GetPointInDirection((Vector2Int)pos, direction);
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
            InitializeMazeCellsInRoomGrid(grid, room.GetBounds());

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

                    Tile BetweenTile = tileGrid.GetTile((currentCell.X + neighborCell.X) / 2,
                                                        (currentCell.Y + neighborCell.Y) / 2);
                    BetweenTile.SetType(TileType.Debug_Technical2);
                    currentCell = neighborCell;
                    break;
                }

                //If the queue is empty, but cells is not, there is a part of the room that is 'contiguous' tile-wise
                //But cannot be contiguous labyrinth-wise, this code still lets those other parts be 'labyrinth'ed
                if (unvisitedCellQueue.Count != 0) continue;
                else if (cells.Count == 0) break;
                else if (cells.Count > 0) currentCell = cells[random.NextInt(0, cells.Count)];
            }

            CovertTechnicalTiles();
        }
    }
}