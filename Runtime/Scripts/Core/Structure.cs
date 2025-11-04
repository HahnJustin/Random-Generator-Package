// File: Structure.cs
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Core
{
    /// <summary>
    /// Pure runtime structure: immutable tiles buffer (flattened),
    /// Width/Height come from the authored asset.
    /// </summary>
    public sealed class Structure : IEnumerable<int>
    {
        private readonly int[] _tiles;

        public int Width { get; }
        public int Height { get; }
        public int Length => _tiles.Length;

        public Structure(int[] tiles, int width, int height)
        {
            _tiles = tiles ?? Array.Empty<int>();
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }

        public int this[int index] => _tiles[index];

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_tiles).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
