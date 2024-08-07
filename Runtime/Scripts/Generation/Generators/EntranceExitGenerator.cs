using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Generators
{
    public class EntranceExitGenerator : OccupanceGenerator
    {
        protected new EntranceExitConfig config;

        public EntranceExitGenerator(EntranceExitConfig config) : base(config)
        {
            this.config = config;
            OutOfBoundsOccupancy = 0;
        }

        protected override void Enact()
        {
            Vector2Int vector1;
            Vector2Int vector2;

            //Creating points diagonally opposed
            if (config.Positioning == EEPositionType.Diagonal_Opposites)
            {
                int diagonality = random.NextInt(0, 2);
                if (diagonality == 1)
                {
                    vector1 = Vector2Int.zero;
                    vector2 = new Vector2Int(width - 1, height - 1);
                }
                else
                {
                    vector1 = new Vector2Int(width - 1, 0);
                    vector2 = new Vector2Int(0, height - 1);
                }
            }
            else if (config.Positioning == EEPositionType.Any_Opposite)
            {
                float a = random.NextFloat();
                float r = Mathf.Sqrt(Mathf.Pow(width,2) + Mathf.Pow(height,2));
                float x = r * Mathf.Cos(a);
                float y = r * Mathf.Sin(a);
                Vector2 first = new (x,y);
                Vector2 second = -first;

                first += new Vector2Int(Mathf.RoundToInt(width * 0.5f), Mathf.RoundToInt(height * 0.5f));
                second += new Vector2Int(Mathf.RoundToInt(width * 0.5f), Mathf.RoundToInt(height * 0.5f));

                vector1 = new Vector2Int(Mathf.RoundToInt(Mathf.Clamp(first.x, 0, width)), Mathf.RoundToInt(Mathf.Clamp(first.y, 0, height)));
                vector2 = new Vector2Int(Mathf.RoundToInt(Mathf.Clamp(second.x, 0, width)), Mathf.RoundToInt(Mathf.Clamp(second.y, 0, height)));
            }
            else
            {
                vector1 = new Vector2Int(random.NextInt(width), random.NextInt(height));
                vector2 = new Vector2Int(random.NextInt(width), random.NextInt(height));
            }

            //Setting exit and entrance arbitrarily to one of the points
            int whichIsExit = random.NextInt(0, 2);
            Vector2Int entranceBorder = whichIsExit == 1 ? vector1 : vector2;
            Vector2Int exitBorder = whichIsExit == 1 ? vector2 : vector1;

            Vector2Int entrancePos;
            Vector2Int entranceAir;
            Vector2Int exitPos;
            Vector2Int exitAir;

            if (config.Placeable == EEPlaceableType.One_Side_Wall)
            {
                TileGrid.ClearNumbers();
                //Creates grid of applicalbe wall tiles
                int[,] grid = new int[width, height];
                foreach (Tile tile in TileGrid)
                {
                    if (!tile.ContainsType(config.SpawnInTile)) continue;
                    int x = tile.x;
                    int y = tile.y;

                    int cardinal = 0;
                    int corner = 0;

                    Vector2Int unoccCard = Constants.OutsideGridVectorInt;
                    Vector2Int unoccCorn = Constants.OutsideGridVectorInt;
                    Vector2Int unoccCorn2 = Constants.OutsideGridVectorInt;

                    List<Tile> neighbors = TileGrid.GetEightNeighborTiles(tile);

                    foreach (Tile neighbor in neighbors)
                    {
                        if (IsOccupied(neighbor) == 1)
                        {
                            if (neighbor.x == x || neighbor.y == y) cardinal += 1;
                            else corner += 1;
                        }
                        else
                        {
                            if (neighbor.x == x || neighbor.y == y) unoccCard = neighbor.Vector;
                            else if (unoccCorn == null) unoccCorn = neighbor.Vector;
                            else if (unoccCorn2 == null) unoccCorn2 = neighbor.Vector;
                        }
                    }

                    if (cardinal == 3 && corner >= 2 && neighbors.Count > 6 &&
                       (((unoccCorn == Constants.OutsideGridVectorInt || unoccCard.x == unoccCorn.x) && 
                       (unoccCorn2 == Constants.OutsideGridVectorInt || unoccCard.x == unoccCorn2.x)) ||
                       ((unoccCorn == Constants.OutsideGridVectorInt || unoccCard.y == unoccCorn.y) && 
                       (unoccCorn2 == Constants.OutsideGridVectorInt || unoccCard.y == unoccCorn2.y))))
                    {
                        grid[x, y] = 1;
                        TileGrid.SetTileValue(x,y,1);
                    }
                }

                entrancePos = grid.GetNearestPosition(entranceBorder, 1);
                entranceAir = TileGrid.GetNearestPosition(entrancePos, TileType.Wall_Object_NA);

                exitPos = grid.GetNearestPosition(exitBorder, 1);
                exitAir = TileGrid.GetNearestPosition(exitPos, TileType.Wall_Object_NA);

                Debug.Log("ent:" + entranceBorder + " " + entrancePos + " air: " + entranceAir);
                Debug.Log("ext:" + exitBorder + " " + exitPos + " air: " + exitAir);
            }
            else
            {
                //Finding nearest air tile, then nearest wall then spawing entrance/exit
                entranceAir = TileGrid.GetNearestPosition(entranceBorder, TileType.Wall_Object_NA);
                exitAir = TileGrid.GetNearestPosition(exitBorder, TileType.Wall_Object_NA);

                entrancePos = TileGrid.GetNearestPosition(entranceAir, config.SpawnInTile);
                exitPos = TileGrid.GetNearestPosition(exitAir, config.SpawnInTile);
            }


            //Couldn't find one of the given tiles
            if (exitAir == Constants.OutsideGridVectorInt ||
                entrancePos == Constants.OutsideGridVectorInt ||
                exitPos == Constants.OutsideGridVectorInt)
            {
                return;
            }

            TileGrid.SetTileType(entrancePos, TileType.Object_Entrance);
            TileGrid.SetTileType(exitPos, TileType.Object_Exit);

            //Add Entrance Exit to Universal Mask
            if (config.AddEntranceExitToMask)
            {
                TileGrid.AddExcludedPosition(entrancePos);
                TileGrid.AddExcludedPosition(exitPos);
            }

            if (!config.CreatePath) return;

            //Create Path between entrance and air next to exit
            AStar astar = new();
            List<Vector2Int> path = astar.FindPath(TileGrid, GetUtil(), entrancePos, exitAir);
            foreach (Vector2Int pos in path)
            {
                if (config.DebugPath) {
                    TileGrid.GetTile(pos).SetType(TileType.Debug_Path_Green);
                }
                if (config.AddPathToMask)
                {
                    TileGrid.AddExcludedPosition(pos);
                }
            }
        }
    }
}
