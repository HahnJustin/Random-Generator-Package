using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Random
{
    public abstract class AbstractRandom
    {
        public AbstractRandom()
        {

        }

        public AbstractRandom(uint seed) : this()
        {
            InitState(seed);
        }

        public abstract void InitState(uint seed);

        public abstract float NextFloat();

        public abstract float NextFloat(float max);

        public abstract float NextFloat(float min, float max);

        public abstract int NextInt();

        public abstract int NextInt(int max);

        public abstract int NextInt(int min, int max);

        public abstract uint NextUInt();

        public abstract uint NextUInt(uint max);

        public abstract uint NextUInt(uint min, uint max);

        public Vector2 InsideUnitCircle()
        {
            int angle = NextInt(360);
            float amount = NextFloat();
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            return new Vector2(x * amount, y * amount);
        }
    }
}