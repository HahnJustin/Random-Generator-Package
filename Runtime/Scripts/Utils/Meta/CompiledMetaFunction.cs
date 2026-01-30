using Dalichrome.RandomGenerator.Core;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public readonly struct CompiledMetaFunction
    {
        public readonly FixedString64Bytes OutputKey;
        public readonly int OutputZ;
        private readonly MetaFunctionNode _root;

        internal CompiledMetaFunction(FixedString64Bytes outputKey, int outputZ, MetaFunctionNode root)
        {
            OutputKey = outputKey;
            OutputZ = outputZ;
            _root = root;
        }

        /// <summary>Evaluate expression at (x,y) and return rounded int.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(ref TileGrid grid, int x, int y)
        {
            try
            {
                float v = _root.Eval(ref grid, x, y);
                return (int)math.round(v);
            }
            catch (Exception ex)
            {
                // Log context for fast debugging (key + z + position)
                Debug.LogError(
                    $"MetaFunction failed while evaluating. " +
                    $"outputKey='{OutputKey.ToString()}', outputZ={OutputZ}, pos=({x},{y}).\n{ex}"
                );

                // Re-throw so your normal error handling still triggers
                throw;
            }
        }

        /// <summary>Evaluate and write output to meta.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(ref TileGrid grid, int x, int y)
        {
            try
            {
                int v = Evaluate(ref grid, x, y);
                grid.AddData(x, y, OutputZ, OutputKey, v);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"MetaFunction failed while running. " +
                    $"outputKey='{OutputKey.ToString()}', outputZ={OutputZ}, pos=({x},{y}).\n{ex}"
                );
                throw;
            }
        }
    }
}
