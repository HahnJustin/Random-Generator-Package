using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator
{
    public abstract class AbstractOperationData
    {
        public CancellationToken Token { get; set; }

        public long OverallOperationMilliseconds { get; set; }

        private Dictionary<int,long> operationsMilliseconds = new();

        public AbstractRandom Random { get { return _random; } }
        private AbstractRandom _random;

        public uint Seed
        {
            get
            {
                return _seed;
            }
            set
            {
                _seed = value;
                _random = new CSharpNativeRandom(value);
            }
        }
        private uint _seed;

        public void AddOperationTime(AbstractConfig config, long ms)
        {
            operationsMilliseconds.Add(config.GetHashCode(), ms);
        }

        public void AddOperationTime(int hashCode, long ms)
        {
            operationsMilliseconds.Add(hashCode, ms);
        }

        public long GetOperationTime(AbstractConfig config)
        {
            if (operationsMilliseconds.TryGetValue(config.GetHashCode(), out long ms))
                return ms;
            return -1;
        }

        public long GetOperationTime(int configHash)
        {
            if (operationsMilliseconds.TryGetValue(configHash, out long ms))
                return ms;
            return -1;
        }

        public Dictionary<int, long> GetOperationMillisDictionary() 
        {
            return operationsMilliseconds;
        }
    }
}