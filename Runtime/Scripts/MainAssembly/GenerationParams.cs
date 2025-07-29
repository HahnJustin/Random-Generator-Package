using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Nodes;

namespace Dalichrome.RandomGenerator.Data
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
        [Condition("IsSeeded", true), SerializeField] private uint seed = 0;

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

        public GeneratorGraph Graph
        {
            get { return graph; }
            set { graph = value; }
        }
        [SerializeReference] private GeneratorGraph graph = null;

        public object Clone()
        {
            GenerationParams newParams = new();
            newParams.height = height;
            newParams.width = width;
            newParams.seed = seed;
            newParams.isSeeded = isSeeded;

            if (graph != null) newParams.graph = graph.Clone();

            return newParams;
        }

        public Generation ToGeneration()
        {
            Generation generation = new (width, height, seed);
            return generation;
        }
    }
}
