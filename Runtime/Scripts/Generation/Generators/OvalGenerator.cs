using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class OvalGenerator : AbstractGenerator
    {
        protected new OvalConfig config;

        public OvalGenerator(OvalConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            float xOrg = width / 2f;
            float yOrg = height / 2f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = TileGrid.GetTile(x,y);
                    //Equation for testing if point is in the interior of an oval
                    if (Mathf.Pow(x - xOrg, 2) / Mathf.Pow(xOrg, 2) + 
                        Mathf.Pow(y - yOrg, 2) / Mathf.Pow(yOrg, 2) <= config.Radius)
                    {
                        tile.SetType(config.Interior);
                    }
                    else
                    {
                        tile.SetType( config.Exterior);
                    }
                }
            }
        }
    }
}
