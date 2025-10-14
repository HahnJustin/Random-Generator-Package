using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class UniversalMaskGenerator : AbstractGenerator<UniversalMaskConfig>
    {
        public UniversalMaskGenerator(UniversalMaskConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            foreach (int2 pos in TileGrid.GetPositions())
            {
                foreach (int type in config.AddToUniversalMaskTiles)
                {
                    if (TileGrid.ColumnContainsId(pos, type))
                    {
                        TileGrid.AddExcludedPosition(pos);
                        break;
                    }
                }     
            }
            return input;
        }
    }
}
