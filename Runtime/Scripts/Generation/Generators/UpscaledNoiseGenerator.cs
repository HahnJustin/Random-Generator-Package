using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class UpscaledNoiseGenerator : AbstractGenerator
    {
        protected new UpscaledNoiseConfig config;

        private int SCALE_AMOUNT = 2;

        public UpscaledNoiseGenerator(UpscaledNoiseConfig config) : base(config)
        {
            this.config = config;
        }

        private int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private int IsOccupied(int[,] grid, int x, int y)
        {
            if (grid.GetLength(0) <= x || x < 0 || grid.GetLength(1) <= y || y < 0)
            {
                if (config.WrapBounds)
                {
                    return IsOccupied(grid, Mod(x, grid.GetLength(0)), Mod(y, grid.GetLength(1)));
                }

                return config.OutOfBoundsOccupied ? 1 : 0;
            }

            return grid[x, y];
        }

        private int GetIfOccupiedTileNextToPosition(int[,] grid, int x, int y)
        {
            int movement = 1;
            int neighbors = 0;
            neighbors += IsOccupied(grid, x, y); // Check Position if occupied
            neighbors += IsOccupied(grid, x - movement, y); // Check left
            neighbors += IsOccupied(grid, x - movement, y - movement);// Check down left
            neighbors += IsOccupied(grid, x, y - movement); // Check down
            neighbors += IsOccupied(grid, x + movement, y - 1); // Check down right
            neighbors += IsOccupied(grid, x + movement, y);// Check right
            neighbors += IsOccupied(grid, x + movement, y + movement); // Check Up Right
            neighbors += IsOccupied(grid, x, y + movement); // Check up
            neighbors += IsOccupied(grid, x - movement, y + movement); //Check up left
            return neighbors;
        }

        protected override void Enact()
        {
            int currentWidth = Mathf.Clamp(Mathf.FloorToInt(width * config.BaseNoiseRatio), 1, width);
            int currentHeight = Mathf.Clamp(Mathf.FloorToInt(height * config.BaseNoiseRatio), 1, height);

            Debug.Log("Base: " + new Vector2Int(currentWidth, currentHeight));

            int occupiedCount = 0;
            int current = 0;

            //Create base grid
            int[,] baseGrid = new int[currentWidth, currentHeight];
            for (int x = 0; x < currentWidth; x++)
            {
                for (int y = 0; y < currentHeight; y++)
                {
                    float modifier = config.ForceDensity ? (current * config.Density / occupiedCount) : 1;
                    current += 1;
                        if (random.NextFloat() < config.Density * modifier)
                        {
                            occupiedCount += 1;
                            baseGrid[x, y] = 1;
                        }
                }
            }

            //Create the upscaled grids
            while (currentWidth < width || currentHeight < height) 
            {
                currentWidth *= SCALE_AMOUNT;
                currentHeight *= SCALE_AMOUNT;

                int[,] nextGrid = new int[currentWidth, currentHeight];
                for (int x = 0; x < currentWidth; x++)
                {
                    for (int y = 0; y < currentHeight; y++)
                    {
                        int baseX = Mathf.FloorToInt(x / (float)SCALE_AMOUNT);
                        int baseY = Mathf.FloorToInt(y / (float)SCALE_AMOUNT);
                        int parentValue = baseGrid[baseX, baseY];
                        if (random.NextFloat() < 
                           (0.5f + (parentValue + GetIfOccupiedTileNextToPosition(baseGrid, baseX, baseY) - 4.5f) * config.LastGridImpact))
                        {
                            nextGrid[x, y] = 1;
                        }
                    }
                }

                baseGrid = nextGrid;
                CancelCheck();
            }

            Debug.Log("Final: " + new Vector2Int(currentWidth, currentHeight));

            //Fill in TileGrid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if(x >= baseGrid.GetLength(0) && y >= baseGrid.GetLength(1)) continue;
                    if(baseGrid[x,y] == 1) TileGrid.SetTileType(x, y, config.FillTile);
                }
            }
        }
    }
}
