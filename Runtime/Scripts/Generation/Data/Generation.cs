using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator
{
    public class Generation : AbstractGridOperationData, IDisposable
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static int _nextId;                    // thread-safe
        private readonly int _id;
        private bool _disposed;
#endif

        public Generation() : base()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _id = Interlocked.Increment(ref _nextId);
            Debug.Log($"[GI #{_id}] ctor (empty)");
#endif
        }

        public Generation(AbstractGridOperationData data) : base(data) { }

        public Generation(GenerationParams genParams)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _id = Interlocked.Increment(ref _nextId);
#endif

            Grid = new(genParams.Width, genParams.Height);
            Seed = genParams.Seed;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ~Generation()
        {
            if (!_disposed && Grid != null)
            {
                Debug.LogError($"[GI #{_id}] FINALIZER — leaked GenerationInfo! seed={Seed}");
            }
            else
            {
                Debug.Log($"[GI #{_id}] FINALIZER — no worries ! seed={Seed}");
            }
        }
#endif
    }
}