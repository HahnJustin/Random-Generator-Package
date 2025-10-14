using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public readonly struct ManagedTileColumn : ITileColumn
    {
        private readonly int[] _arr;
        private readonly int _offset;
        private readonly int _length;
        private readonly byte _valid; // 1==true, 0==false (internal flag)

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
            get => _length;
        }

        public int this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _arr[_offset + i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedTileColumn(int[] array, int offset, int length, int x, int y)
        {
            _arr = array ?? System.Array.Empty<int>();
            _offset = offset;
            _length = length;
            X = x;
            Y = y;
            _valid = (byte)((length > 0 && _arr.Length >= offset + length) ? 1 : 0);
        }

        // Convenience (keeps DIM out of the interface)
        public int2 ToInt2() => new int2(X, Y);

        // Optional: easy ginvalidh factory
        public static ManagedTileColumn Invalid => new ManagedTileColumn(System.Array.Empty<int>(), 0, 0, 0, 0);
    }
}
