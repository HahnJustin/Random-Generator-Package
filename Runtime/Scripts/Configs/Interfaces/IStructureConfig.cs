using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    public interface IStructureConfig
    {
        [SerializeField] public string StructureTableId { get; set; }

        [HideInInspector] public StructureTable StructureTable { get; set; }
    }
}