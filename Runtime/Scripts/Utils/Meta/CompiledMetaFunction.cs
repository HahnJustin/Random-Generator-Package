using Dalichrome.RandomGenerator.Core;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public readonly struct CompiledMetaFunction
    {
        public readonly FixedString64Bytes OutputKey;
        public readonly int OutputZ;

        private readonly MetaBytecodeProgram _program;

        internal CompiledMetaFunction(FixedString64Bytes outputKey, int outputZ, MetaBytecodeProgram program)
        {
            OutputKey = outputKey;
            OutputZ = outputZ;
            _program = program;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(ref TileGrid grid, int x, int y)
        {
            return _program.Eval(ref grid, x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(ref TileGrid grid, int x, int y)
        {
            float v = Evaluate(ref grid, x, y);
            grid.AddData(x, y, OutputZ, OutputKey, v);
        }
    }
}
