using System;

namespace Dalichrome.RandomGenerator.Utils
{
    internal abstract class FloatConditionNode
    {
        internal abstract bool Evaluate(float value);
    }
}