using System;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    internal struct MetaKey : IEquatable<MetaKey>
    {
        public int3 pos;
        public ulong field;

        public bool Equals(MetaKey other)
        {
            return pos.Equals(other.pos) && field == other.field;
        }

        public override bool Equals(object obj)
        {
            return obj is MetaKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = pos.GetHashCode();
                hash = (hash * 397) ^ field.GetHashCode();
                return hash;
            }
        }
    }
}