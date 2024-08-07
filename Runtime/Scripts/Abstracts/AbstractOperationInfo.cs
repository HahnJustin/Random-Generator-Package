using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    public abstract class AbstractOperationInfo
    {
        public CancellationToken Token { get; set; }
        public virtual TileGrid Grid { get; set; }

        public long OverallOperationMilliseconds { get; set; }

        private List<long> operationsMilliseconds = new();

        public void AddOperationTime(long ms)
        {
            operationsMilliseconds.Add(ms);
        }

        public long GetOperationTime(int index)
        {
            if (index < 0 || index >= operationsMilliseconds.Count) return -1;
            return operationsMilliseconds[index];
        }
    }
}