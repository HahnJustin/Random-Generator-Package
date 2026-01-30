using System;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IMetaFunctionConfig
    {
        [SerializeField] public MetaFunction MetaFunction { get; set; }
    }
}