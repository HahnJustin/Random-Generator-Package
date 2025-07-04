using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Generators
{
    public class MapJoiner : AbstractJoiner<MapJoinerConfig>
    {
        protected MapJoiner(MapJoinerConfig config) : base(config) { }

        protected override Generation Join(List<Generation> generations)
        {
            Generation main = generations[0];
            generations.RemoveAt(0);

            TileGrid grid = main.Grid;
            grid.RemoveRegion();

            foreach (Generation generation in generations)
            {
                TileGrid otherGrid = generation.Grid;
                foreach (Tile tile in otherGrid)
                {
                    grid.SetTile(tile.Int2, tile);
                }
                otherGrid.RemoveRegion();
            }
            return main;
        }
    }
}
