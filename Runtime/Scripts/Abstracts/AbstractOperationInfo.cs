using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;
using System;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace Dalichrome.RandomGenerator
{
    public abstract class AbstractOperationInfo : IDisposable
    {
        public CancellationToken Token { get; set; }

        private TileGrid grid;
        public TileGrid Grid
        {
            get
            {
                return grid;
            }

            set
            {
                if (grid != value) Dispose();
                grid = value;
            }
        }

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

        public void Dispose()
        {
            if (grid != null && grid.IsDataValid) grid.Dispose();
        }
    }
}