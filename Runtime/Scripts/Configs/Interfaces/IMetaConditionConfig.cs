using System;
using System.ComponentModel;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IMetaConditionConfig
    {
        [SerializeField] public MetaCondition MetaCondition { get; set; }
    }
}