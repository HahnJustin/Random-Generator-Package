using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Dalichrome.RandomGenerator.Data
{
    public class AbstractGridOperationData : AbstractOperationData, IDisposable
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        protected static int _nextId;                    // thread-safe
        protected readonly int _id;
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
            data._disposed = true;
            Grid = data.Grid;
            Seed = data.Seed;
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
                Debug.LogError($"[GridOpData #{_id}] FINALIZER — leaked Generation! seed={Seed}");
            }
        }
#endif
    }
}