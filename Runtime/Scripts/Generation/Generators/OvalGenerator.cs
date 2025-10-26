using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class OvalGenerator : AbstractGenerator<OvalConfig>
    {
        public OvalGenerator(OvalConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            float xOrg = width / 2f;
            float yOrg = height / 2f;

            foreach (int2 pos in TileGrid.GetPositions()) 
            {
                //Equation for testing if point is in the interior of an oval
                if (Mathf.Pow(pos.x - xOrg, 2) / Mathf.Pow(xOrg, 2) + 
                    Mathf.Pow(pos.y - yOrg, 2) / Mathf.Pow(yOrg, 2) <= config.Radius)
                {
                    TileGrid.SetTileId(pos, (int)config.Interior);
                }
                else
                {
                    TileGrid.SetTileId(pos, (int)config.Exterior);
                }
                
            }
            return input;
        }
    }
}
