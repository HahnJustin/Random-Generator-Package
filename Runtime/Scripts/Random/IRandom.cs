using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Random
{
    public interface IRandom
    {
        public int NextInt();

        public int NextInt(int max);

        public int NextInt(int min, int max);

        public float NextFloat();

        public float NextFloat(float max);

        public float NextFloat(float min, float max);

        public uint NextUint();

        public uint NextUint(uint max);

        public uint NextUint(uint min, uint max);
    }
}