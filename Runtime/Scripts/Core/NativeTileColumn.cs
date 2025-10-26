using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    /// <summary>
    /// Burst/job-friendly column view of Z values at (X,Y).
    /// All fields are blittable. Default(NativeTileColumn) is invalid.
    /// </summary>
    public readonly struct NativeTileColumn : ITileColumn
    {
        private readonly NativeSlice<int> _slice;
        private readonly int _valid; // 1 = valid, 0 = invalid

        public int X { get; }
        public int Y { get; }
        public int2 Int2 => new int2(X, Y);

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _valid != 0;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _slice.Length;
        }

        public int this[int z]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _slice[z];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<int> AsSlice() => _slice;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeTileColumn(NativeSlice<int> slice, int x, int y)
        {
            _slice = slice;
            X = x;
            Y = y;
            _valid = 1;
        }

        public static NativeTileColumn Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new NativeTileColumn(default, 0, 0); // _valid = 0
        }
    }
}
