using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Generators
{
    public static class Initializers
    {
        public static readonly AbstractInitializer[] All =
        {
            new MetaInitializer(),
            // new OtherFinalizer(),
        };

        public static Generation Initialize(Generation generation, List<AbstractConfig> configs)
        {
            foreach (AbstractInitializer init in All)
            {
                generation = init.Execute(generation, configs);
            }
            return generation;
        }
    }
}