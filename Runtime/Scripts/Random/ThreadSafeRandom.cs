using System;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Random
{
    public class ThreadSafeRandom
    {
        private static UnityMathematicsRandom globalRandom = new();
        private static readonly object globalLock = new();

        public static void InitState()
        {
            lock (globalLock)
            {
                globalRandom = new();
            }
        }

        public static void InitState(uint seed)
        {
            lock (globalLock)
            {
                globalRandom.InitState(seed);
            }
        }

        public static int NextInt()
        {
            lock (globalLock)
            {
                return globalRandom.NextInt();
            }
        }

        public static int NextInt(int max)
        {
            lock (globalLock)
            {
                return globalRandom.NextInt(max);
            }
        }

        public static int NextInt(int min, int max)
        {
            lock (globalLock)
            {
                return globalRandom.NextInt(min, max);
            }
        }

        public static uint NextUInt()
        {
            lock (globalLock)
            {
                return globalRandom.NextUInt();
            }
        }

        public static uint NextUInt(uint max)
        {
            lock (globalLock)
            {
                return globalRandom.NextUInt(max);
            }
        }

        public static uint NextUInt(uint min, uint max)
        {
            lock (globalLock)
            {
                return globalRandom.NextUInt(min, max);
            }
        }
    }
}