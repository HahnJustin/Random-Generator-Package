using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class CellularAutomataGenerator : OccupanceGenerator
    {
        protected new CellularAutomataConfig config;

        public CellularAutomataGenerator(CellularAutomataConfig config) : base(config)
        {
            this.config = config;
            OutOfBoundsOccupancy = this.config.BorderOccupied ? 1 : 0;
        }

        protected override void Enact()
        {
            bool noProbability = config.PlaceProbability >= 1;

            for (int i = 0; i < config.Repetitions; i++)
            {
                int[,] caBuffer = new int[width, height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Tile tile = TileGrid.GetTile(x,y);
                        int liveCellCount = IsOccupied(tile) + GetNeighbourCellCount(x, y);
                        caBuffer[x, y] = liveCellCount > config.LiveNeighboursRequired ? 1 : 0;
                    }
                }

                for (int x = 0; x < width; ++x)
                {
                    for (int y = 0; y < height; ++y)
                    {
                        if (caBuffer[x, y] == 1 && (noProbability || random.NextFloat() < config.PlaceProbability))
                        {
                            TileGrid.SetTileType(x, y, config.Fill);
                        }
                        else{
                            TileGrid.SetTileType(x, y, config.Empty);
                        }
                    }
                }
            }
        }

        private int GetNeighbourCellCount(int x, int y)
        {
            int neighbourCellCount = 0;

            neighbourCellCount += IsOccupied(x - 1, y);
            neighbourCellCount += IsOccupied(x - 1, y - 1);

            neighbourCellCount += IsOccupied(x, y - 1);
            neighbourCellCount += IsOccupied(x + 1, y - 1);

            neighbourCellCount += IsOccupied(x + 1, y);
            neighbourCellCount += IsOccupied(x + 1, y + 1);

            neighbourCellCount += IsOccupied(x, y + 1);
            neighbourCellCount += IsOccupied(x - 1, y + 1);

            return neighbourCellCount;
        }
    }
}
