using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Core;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class LimitTileLayerAttribute : System.Attribute
    {
        public LayerType layer;
        public LimitTileLayerAttribute(LayerType layer)
        {
            this.layer = layer;
        }
    }
}