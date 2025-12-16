using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Generators
{
    public static class Finalizers
    {
        public static readonly AbstractFinalizer[] All =
        {
            new VarianceFinalizer(),
            // new OtherFinalizer(),
        };

        public static Generation Finalize(Generation generation)
        {
            foreach (AbstractFinalizer fin in All)
            {
                generation = fin.Execute(generation);
            }
            return generation;
        }
    }
}