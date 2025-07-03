using System;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IJoinConfig
    {
        [SerializeField, DefaultValue(1)] public int InputCount { get; set; }
    }
}
