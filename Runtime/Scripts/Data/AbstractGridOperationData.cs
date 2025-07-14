using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;


namespace Dalichrome.RandomGenerator.Data
{
    public class AbstractGridOperationData : AbstractOperationData, IDisposable
    {
        private static readonly bool ParallelProcessing = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        protected static int _nextId;                    // thread-safe
        public readonly int _id;
        protected bool _disposed;
#endif

        public int Width { get { return Grid.width; } }

        public int Height { get { return Grid.height; } }

        private TileGrid grid;
        public TileGrid Grid
        {
            get
            {
                return grid;
            }

            set
            {
                if (grid != null && grid != value && grid.IsValid)
                {
                    grid.Dispose();
                }
                grid = value;
            }
        }

        public bool Valid =>  grid != null && grid.IsValid;

        public int2 Minimum
        {
            get { return Grid.Minimum; }
        }

        public int2 Maximum
        {
            get { return Grid.Maximum; }
        }

        public AbstractGridOperationData()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _id = Interlocked.Increment(ref _nextId);
            Debug.Log($"[GridOpData #{_id}] ctor (empty)");
#endif
        }

        public AbstractGridOperationData(AbstractGridOperationData data)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _id = Interlocked.Increment(ref _nextId);
            Debug.Log($"[GridOpData #{_id}] ctor data");
#endif
            data.ParallelDispose();

            Grid = data.Grid;
            Seed = data.Seed;
        }

        public void ParallelDispose()
        {
            if (ParallelProcessing) { Dispose(); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else _disposed = true;
            Debug.Log($"[GridOpData #{_id}] paralllel disposed");
#endif
        }

        public void Dispose()
        {
            if (grid != null && grid.IsValid)
            {
                Debug.Log($"[GridOpData #{_id}] disposing seed={Seed}");
                grid.Dispose();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _disposed = true;
#endif
            }
        }

        public void AddLayersLookups(Dictionary<int, LayerType> layerLookup)
        {
            Grid.AddLayersLookups(layerLookup);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ~AbstractGridOperationData()
        {
            if (!_disposed && Grid != null)
            {
                Debug.LogError($"[GridOpData #{_id}] FINALIZER — leaked {GetType()}! seed={Seed}");
            }
        }
#endif
    }
}