using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class EntranceExitGenerator : AbstractGenerator<EntranceExitConfig>
    {
        private OccupanceUtil util;

        public EntranceExitGenerator(EntranceExitConfig config) : base(config)
        {
            this.config = config;
            util = new(config) { OutOfBoundsOccupancy = 0 };
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
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
                float a = random.NextFloat() * Mathf.PI * 2;
                float r = Mathf.Sqrt(Mathf.Pow(width,2) + Mathf.Pow(height,2));
                float x = r * Mathf.Cos(a);
                float y = r * Mathf.Sin(a);
                Vector2 first = new (x,y);
                Vector2 second = -first;

                first += new Vector2Int(Mathf.RoundToInt(width * 0.5f), Mathf.RoundToInt(height * 0.5f));
                second += new Vector2Int(Mathf.RoundToInt(width * 0.5f), Mathf.RoundToInt(height * 0.5f));

                vector1 = new Vector2Int(Mathf.RoundToInt(Mathf.Clamp(first.x, 0, width - 1)), Mathf.RoundToInt(Mathf.Clamp(first.y, 0, height - 1)));
                vector2 = new Vector2Int(Mathf.RoundToInt(Mathf.Clamp(second.x, 0, width - 1)), Mathf.RoundToInt(Mathf.Clamp(second.y, 0, height - 1)));
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
                //Creates readGrid of applicalbe wall tiles
                int[,] grid = new int[width, height];
                foreach (int2 pos in TileGrid.GetPositions())
                {
                    if (!TileGrid.ColumnContainsId(pos, config.SpawnInTile)) continue;
   
                    int cardinal = 0;
                    int corner = 0;

                    int2 unoccCard = Constants.OutsideGridInt2;
                    int2 unoccCorn = Constants.OutsideGridInt2;
                    int2 unoccCorn2 = Constants.OutsideGridInt2;

                    List<int2> neighbors = TileGrid.GetEightNeighborPositions(pos);

                    foreach (int2 neighbor in neighbors)
                    {
                        if (util.IsOccupied(neighbor) == 1)
                        {
                            if (neighbor.x == pos.x || neighbor.y == pos.y) cardinal += 1;
                            else corner += 1;
                        }
                        else
                        {
                            if (neighbor.x == pos.x || neighbor.y == pos.y) unoccCard = neighbor;
                            else if (math.all(unoccCorn == Constants.OutsideGridInt2)) unoccCorn = neighbor;
                            else if (math.all(unoccCorn2 == Constants.OutsideGridInt2)) unoccCorn2 = neighbor;
                        }
                    }

                    if (cardinal == 3 && corner >= 2 && neighbors.Count > 6 &&
                       (((math.all(unoccCorn == Constants.OutsideGridInt2) || unoccCard.x == unoccCorn.x) && 
                       (math.all(unoccCorn2 == Constants.OutsideGridInt2) || unoccCard.x == unoccCorn2.x)) ||
                       ((math.all(unoccCorn == Constants.OutsideGridInt2) || unoccCard.y == unoccCorn.y) && 
                       (math.all(unoccCorn2 == Constants.OutsideGridInt2)|| unoccCard.y == unoccCorn2.y))))
                    {
                        grid[pos.x, pos.y] = 1;
                        TileGrid.SetTileValue(pos,1);
                    }
                }

                entrancePos = grid.GetNearestPosition(entranceBorder, 1);
                entranceAir = TileGrid.GetNearestPosition(entrancePos, (int)TileDefaults.Wall_Object_NA);

                exitPos = grid.GetNearestPosition(exitBorder, 1);
                exitAir = TileGrid.GetNearestPosition(exitPos, (int)TileDefaults.Wall_Object_NA);

                Debug.Log("ent:" + entranceBorder + " " + entrancePos + " air: " + entranceAir);
                Debug.Log("ext:" + exitBorder + " " + exitPos + " air: " + exitAir);
            }
            else
            {
                // TODO this Wall Object NA thing is completely broken, so think hard and fix this
                //Finding nearest air tile, then nearest wall then spawing entrance/exit
                entranceAir = TileGrid.GetNearestPosition(entranceBorder, (int)TileDefaults.Wall_Object_NA);
                exitAir = TileGrid.GetNearestPosition(exitBorder, (int)TileDefaults.Wall_Object_NA);

                entrancePos = TileGrid.GetNearestPosition(entranceAir, (int)config.SpawnInTile);
                exitPos = TileGrid.GetNearestPosition(exitAir, (int)config.SpawnInTile);
            }


            //Couldn't find one of the given tiles
            if (exitAir == Constants.OutsideGridVectorInt ||
                entrancePos == Constants.OutsideGridVectorInt ||
                exitPos == Constants.OutsideGridVectorInt)
            {
                return input;
            }

            TileGrid.SetTileId(entrancePos, (int)TileDefaults.Object_Entrance);
            TileGrid.SetTileId(exitPos, (int)TileDefaults.Object_Exit);

            //Add Entrance Exit to Universal Mask
            if (config.AddEntranceExitToMask)
            {
                TileGrid.AddExcludedPosition(entrancePos);
                TileGrid.AddExcludedPosition(exitPos);
            }

            if (!config.CreatePath) return input;

            //Create Path between entrance and air next to exit
            AStar astar = new();
            List<Vector2Int> path = astar.FindPath(TileGrid, util, entrancePos, exitAir);
            foreach (Vector2Int pos in path)
            {
                if (config.DebugPath) {
                    TileGrid.SetTileId(pos, (int)TileDefaults.Debug_Path_Green);
                }
                if (config.AddPathToMask)
                {
                    TileGrid.AddExcludedPosition(pos);
                }
            }
            return input;
        }
    }
}
