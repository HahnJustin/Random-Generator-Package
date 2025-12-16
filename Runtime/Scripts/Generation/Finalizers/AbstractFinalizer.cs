using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractFinalizer 
    {
        protected NativeLookupBundle bundle;
        protected TileGrid tileGrid;
        protected AbstractRandom random;

        protected abstract bool RunCondition();

        protected abstract Generation Do(Generation generation);

        public Generation Execute (Generation input)
        {
            tileGrid = input.Grid;
            bundle = (NativeLookupBundle)tileGrid.GetLookupBundle();
            random = input.Random;

            if (!RunCondition()) return input;

            return Do(input);
        }
    }
}