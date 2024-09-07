using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Random
{
    public class CSharpNativeRandom : AbstractRandom
    {
        private System.Random random;

        public CSharpNativeRandom() : base()
        {
            random = new System.Random();
        }

        public CSharpNativeRandom(uint seed)
        {
            InitState(seed);
        }

        public override void InitState(uint seed)
        {
            random = new System.Random((int)seed);
        }

        public override float NextFloat()
        {
            return (float)random.NextDouble();
        }

        public override float NextFloat(float max)
        {
            return (float)random.NextDouble() * max;
        }

        public override float NextFloat(float min, float max)
        {
            if (min >= max) return min;
            return (float)random.NextDouble() * (max - min) + min;
        }

        public override int NextInt()
        {
            return random.Next();
        }

        public override int NextInt(int max)
        {
            return random.Next(max);
        }

        public override int NextInt(int min, int max)
        {
            return random.Next(min, max);
        }

        public override uint NextUInt()
        {
            return Convert.ToUInt32(NextInt());
        }

        public override uint NextUInt(uint max)
        {
            return Convert.ToUInt32(random.Next(Convert.ToInt32(max)));
        }

        public override uint NextUInt(uint min, uint max)
        {
            return Convert.ToUInt32(random.Next(Convert.ToInt32(min), Convert.ToInt32(max)));
        }
    }
}