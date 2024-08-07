using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class ConditionAttribute : Attribute
    {
        public string dependentVariable;
        public object objectToEqual;
        public ConditionAttribute(string dependentVariable, object objectToEqual)
        {
            this.dependentVariable = dependentVariable;
            this.objectToEqual = objectToEqual;
        }
    }
}
