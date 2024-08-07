using UnityEngine;
using Unity.Mathematics;
using System;

namespace Dalichrome.RandomGenerator.Random
{
    public class UnityMathematicsRandom : AbstractRandom
    {
        private Unity.Mathematics.Random random;

        public UnityMathematicsRandom() : base()
        {
            random = new((uint)DateTime.Now.Millisecond);
        }

        public UnityMathematicsRandom(uint seed)
        {
            random = new();
            InitState(seed);
        }

        public override void InitState(uint seed)
        {
            random.InitState(seed);
        }

        public override float NextFloat()
        {
            return random.NextFloat();
        }

        public override float NextFloat(float max)
        {
            return random.NextFloat(max);
        }

        public override float NextFloat(float min, float max)
        {
            return random.NextFloat(min, max);
        }

        public override int NextInt()
        {
            return random.NextInt();
        }

        public override int NextInt(int max)
        {
            return random.NextInt(max);
        }

        public override int NextInt(int min, int max)
        {
            return random.NextInt(min, max);
        }

        public override uint NextUInt()
        {
            return random.NextUInt();
        }

        public override uint NextUInt(uint max)
        {
            return random.NextUInt(max);
        }

        public override uint NextUInt(uint min, uint max)
        {
            return random.NextUInt(min, max);
        }
    }
}