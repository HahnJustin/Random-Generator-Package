using System;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class LayerDisplayAttribute : PropertyAttribute { }
}