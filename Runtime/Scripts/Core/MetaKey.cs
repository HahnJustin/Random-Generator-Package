using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    internal readonly struct MetaKey : IEquatable<MetaKey>
    {
        public readonly int3 pos;
        public readonly FixedString64Bytes field;

        public MetaKey(int3 pos, FixedString64Bytes field)
        {
            this.pos = pos;
            this.field = field;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MetaKey other)
            => pos.Equals(other.pos) && field.Equals(other.field);

        public override bool Equals(object obj)
            => obj is MetaKey other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                // Same combine you had; good.
                int hash = pos.GetHashCode();
                hash = (hash * 397) ^ field.GetHashCode();
                return hash;
            }
        }
    }
}