using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    /// <summary>
    /// Draw the field only when the value of <paramref name="dependentPropertyName"/>
    /// equals <paramref name="compareAgainst"/>.
    /// </summary>
    public sealed class ConditionAttribute : PropertyAttribute
    {
        public readonly string DependentPropertyName;
        public readonly object CompareAgainst;

        // The CLR lets us pass enums, ints, strings, bools, floats, etc.
        public ConditionAttribute(string dependentPropertyName, object compareAgainst)
        {
            DependentPropertyName = dependentPropertyName;
            CompareAgainst        = compareAgainst;
        }
    }
}
