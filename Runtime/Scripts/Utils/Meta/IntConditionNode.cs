using System;

namespace Dalichrome.RandomGenerator.Utils
{
    internal abstract class IntConditionNode
    {
        internal abstract bool Evaluate(int value);
    }
}