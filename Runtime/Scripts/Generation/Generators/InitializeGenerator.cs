using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator.Generators
{
    public class InitializeGenerator : AbstractGenerator<InitialConfig>
    {
        public InitializeGenerator(InitialConfig config) : base(config) { }

        protected override void Enact()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileGrid.SetTileId(x, y, config.Ground);
                    TileGrid.SetTileId(x, y, config.Wall);
                    TileGrid.SetTileId(x, y, config.ContainedObject);
                    TileGrid.SetTileId(x, y, config.Debug);
                }
            }
            return;
        }
    }
}
