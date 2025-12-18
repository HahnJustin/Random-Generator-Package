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

        private readonly MetadataEntry[] _meta;

        public int Width { get; }
        public int Height { get; }
        public int Length => _tiles.Length;

        public bool Flippable { get; }
        public bool Rotatable { get; }

        public Structure(int[] tiles, MetadataEntry[] meta, int width, int height, bool flippable, bool rotatable)
        {
            _tiles = tiles ?? Array.Empty<int>();
            _meta = meta ?? Array.Empty<MetadataEntry>();
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Flippable = flippable;
            Rotatable = rotatable;
        }

        public int this[int index] => _tiles[index];

        public IEnumerable<MetadataEntry> GetMetaEnumerable() => _meta;
        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_tiles).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
