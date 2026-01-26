using System;

namespace Dalichrome.RandomGenerator.Utils
{
    internal abstract class KeyConditionNode
    {
        internal abstract bool Evaluate(Func<string, bool> keySatisfied);
    }
}