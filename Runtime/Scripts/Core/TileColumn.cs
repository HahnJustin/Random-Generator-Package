using Unity.Collections;


namespace Dalichrome.RandomGenerator.Core
{
    public struct TileColumn
    {
        private NativeSlice<int> _native;     // when _kind == 1
        private int[] _arr;                   // when _kind == 2
        private int _off, _len;
        private byte _kind;                   // 0=empty, 1=native, 2=managed

        public int Length => _kind == 1 ? _native.Length : _len;
        public int this[int i] => _kind == 1 ? _native[i] : _arr[_off + i];

        public static TileColumn FromNative(NativeSlice<int> s) => new() { _native = s, _kind = 1 };
        public static TileColumn FromManaged(int[] a, int off, int len) => new() { _arr = a, _off = off, _len = len, _kind = 2 };
    }
}