using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class UniversalMaskGenerator : AbstractGenerator
    {
        protected new UniversalMaskConfig config;

        public UniversalMaskGenerator(UniversalMaskConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = TileGrid.GetTile(x, y);

                    foreach (TileType type in config.AddToUniversalMaskTiles)
                    {
                        if (tile.ContainsType(type))
                        {
                            TileGrid.AddExcludedPosition(tile.Vector);
                            break;
                        }
                    }
                }
            }
        }
    }
}
