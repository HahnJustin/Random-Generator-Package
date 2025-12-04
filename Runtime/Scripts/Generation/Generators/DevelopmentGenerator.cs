using System;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class DevelopmentGenerator : AbstractGenerator<DevelopmentConfig>
    {
        private OccupanceUtil util;

        public DevelopmentGenerator(DevelopmentConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (random.NextFloat() < config.Density)
                        TileGrid.AddData(x,y,config.MetaFieldName, random.NextInt(0,10));
                }
            }
            
            return input;
        }
    }
}
