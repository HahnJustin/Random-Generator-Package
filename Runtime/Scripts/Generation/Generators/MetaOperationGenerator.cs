using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Utils;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class MetaOperationGenerator : AbstractGenerator<MetaOperationConfig>
    {
        private MetaFunctionUtil util;

        public MetaOperationGenerator(MetaOperationConfig config) : base(config) 
        {
            util = new MetaFunctionUtil(config);
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            foreach (int2 pos in TileGrid.GetPositions())
            {
                if (!TileGrid.IsInsideMask(pos)) continue;
                util.ApplyFunction(pos.x, pos.y);
            }
            return input;
        }
    }
}
