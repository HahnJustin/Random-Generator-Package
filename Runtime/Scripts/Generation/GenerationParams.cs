using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator
{
    [Serializable]
    public class GenerationParams: ICloneable
    {
        public bool IsSeeded
        {
            get { return isSeeded; }
            set { isSeeded = value; }
        }
        [SerializeField] private bool isSeeded = false;

        public uint Seed 
        {
            get { return seed; }
            set { seed = value; }
        }
        [Condition("IsSeeded", true),SerializeField] private uint seed = 0;

        public int Width
        {
            get { return width; }
            set { width = value; }
        }
        [SerializeField] private int width = 100;

        public int Height
        {
            get { return height; }
            set { height = value; }
        }
        [SerializeField] private int height = 100;

        public List<AbstractGeneratorConfig> Configs
        {
            get { return configs; }
            set { configs = value; }
        }
        [SerializeReference] private List<AbstractGeneratorConfig> configs = new();

        public object Clone()
        {
            GenerationParams newParams = new();
            newParams.height = height;
            newParams.width = width;
            newParams.configs = configs.DeepClone();
            newParams.seed = seed;
            newParams.isSeeded = isSeeded;
            return newParams;
        }
    }
}
