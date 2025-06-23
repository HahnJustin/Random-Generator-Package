using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator.Generators
{
    public class CopyGenerator : AbstractGenerator<CopyConfig>
    {
        public CopyGenerator(CopyConfig config) : base(config) { }

        protected override void Enact()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = TileGrid.GetTile(x, y);

                    foreach (SerialPair<int, int> pair in config.FromTo)
                    {
                        if (tile.ContainsId(pair.Key))
                        {
                            TileGrid.SetTileId(tile, pair.Value);
                        }
                    }
                }
            }
            return;
        }
    }
}
