using System;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public interface IAbstractOperation
    {
        AbstractOperationData Do(AbstractOperationData input);

        Type InputType { get; }
        Type OutputType { get; }
        Type ConfigType { get; }
    }
}
