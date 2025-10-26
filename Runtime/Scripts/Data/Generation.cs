using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Data
{
    public class Generation : AbstractGridOperationData, IDisposable
    {
        public Generation() : base()
        {

        }

        public Generation(AbstractGridOperationData data) : base(data) { }

        public Generation(int width, int height, int depth, uint seed) : base()
        {
            Grid = new(width, height, depth);
            Seed = seed;
        }

        public void ToSerial()
        {
            Grid.ToSerial();
        }
    }
}