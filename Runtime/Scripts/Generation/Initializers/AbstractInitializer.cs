using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractInitializer
    {
        protected NativeLookupBundle bundle;
        protected TileGrid tileGrid;
        protected AbstractRandom random;

        protected List<AbstractConfig> configs;

        protected abstract bool RunCondition();

        protected abstract Generation Do(Generation generation);

        public Generation Execute(Generation input, List<AbstractConfig> configs)
        {
            tileGrid = input.Grid;
            bundle = (NativeLookupBundle)tileGrid.GetLookupBundle();
            random = input.Random;

            this.configs = new List<AbstractConfig>(configs);

            if (!RunCondition()) return input;

            return Do(input);
        }
    }
}