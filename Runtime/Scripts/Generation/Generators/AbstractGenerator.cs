using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractGenerator<C> : AbstractMaskedGridOperation<C, Generation>, IGenerator
        where C : AbstractGeneratorConfig
    {
        protected AbstractGenerator(C config) : base(config) {}

        protected override Generation FailConditionDefault(Generation input)
        {
            return input;
        }
    }
}
