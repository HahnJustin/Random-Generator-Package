using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class CopyGenerator : AbstractGenerator<CopyConfig>
    {
        public CopyGenerator(CopyConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            foreach (int2 pos in TileGrid.GetPositions()) 
            {

                foreach (SerialPair<int, int> pair in config.FromTo)
                {
                    if (TileGrid.ColumnContainsId(pos, pair.Key))
                    {
                        TileGrid.SetTileId(pos, pair.Value);
                    }
                }
            }
            return input;
        }
    }
}
