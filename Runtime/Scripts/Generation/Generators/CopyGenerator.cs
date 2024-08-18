using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class CopyGenerator : AbstractGenerator
    {
        protected new CopyConfig config;

        public CopyGenerator(CopyConfig config) : base(config)
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

                    foreach (SerialPair<TileType,TileType> pair in config.FromTo)
                    {
                        if (tile.ContainsType(pair.Key))
                        {
                            TileGrid.SetTileType(tile, pair.Value);
                        }
                    }
                }
            }
        }
    }
}
