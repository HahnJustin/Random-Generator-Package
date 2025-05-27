using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator
{
    public class GenerationInfo : AbstractOperationInfo, IDisposable
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static int _nextId;                    // thread-safe
        private readonly int _id;
        private bool _disposed;
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
                if (grid != null && grid != value && grid.IsDataValid)
                {
                    grid.Dispose();
                }
                grid = value;
            }
        }

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[GI #{_id}] seedSet  seed={value} stack: {Environment.StackTrace}");
#endif
                _seed = value;
                _random = new CSharpNativeRandom(value);
            }
        }
        private uint _seed;

        public GenerationInfo()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _id = Interlocked.Increment(ref _nextId);
            Debug.Log($"[GI #{_id}] ctor (empty)");
#endif
        }

        public GenerationInfo(GenerationParams genParams)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _id = Interlocked.Increment(ref _nextId);
#endif

            Grid = new(genParams.Width, genParams.Height);
            _seed = genParams.Seed;
            _random = new CSharpNativeRandom(_seed);
        }

        public void Dispose()
        {

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_disposed) return;
            _disposed = true;
#endif
            if (grid != null && grid.IsDataValid)
            {
                Debug.Log($"[GI #{_id}] disposed");
                grid.Dispose();
            }
            else
            {
                Debug.Log($"[GI #{_id}] dispose skipped");
            }

            GC.SuppressFinalize(this);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ~GenerationInfo()
        {
            if (!_disposed && grid != null)
            {
                Debug.LogError($"[GI #{_id}] FINALIZER — leaked GenerationInfo! seed={_seed}");
            }
            else
            {
                Debug.Log($"[GI #{_id}] FINALIZER — no worries ! seed={_seed}");
            }
        }
#endif

        public void AddLayersLookups(Dictionary<int,LayerType> layerLookup)
        {
            Grid.AddLayersLookups(layerLookup);
        }
    }
}