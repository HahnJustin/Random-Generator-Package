using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class BorderGenerator : AbstractGenerator
    {
        protected new BorderConfig config;

        public BorderGenerator(BorderConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if(x < config.Depth || width - x < config.Depth ||
                       y < config.Depth || height - y < config.Depth)
                    {
                        TileGrid.SetTileType(x, y, config.Border);
                    }
                }
            }
        }
    }
}
