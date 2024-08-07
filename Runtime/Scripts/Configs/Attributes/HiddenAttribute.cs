using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class HiddenAttribute : System.Attribute
    {
        public HiddenAttribute()
        {
        }
    }
}