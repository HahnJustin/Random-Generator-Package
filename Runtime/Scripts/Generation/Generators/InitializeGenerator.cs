using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class InitializeGenerator : AbstractGenerator
    {
        protected new InitialConfig config;

        public InitializeGenerator(InitialConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileGrid.SetTileType(x, y, config.Ground);
                    TileGrid.SetTileType(x, y, config.Wall);
                    TileGrid.SetTileType(x, y, config.ContainedObject);
                    TileGrid.SetTileType(x, y, config.Debug);
                }
            }
        }
    }
}
