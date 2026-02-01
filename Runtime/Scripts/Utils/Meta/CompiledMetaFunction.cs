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
        public int Evaluate(ref TileGrid grid, int x, int y)
        {
            float v = _program.Eval(ref grid, x, y);
            return (int)math.round(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(ref TileGrid grid, int x, int y)
        {
            int v = Evaluate(ref grid, x, y);
            grid.AddData(x, y, OutputZ, OutputKey, v);
        }
    }
}
