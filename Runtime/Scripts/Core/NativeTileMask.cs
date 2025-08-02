using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public struct NativeTileMask : ITileMask, IDisposable
    {
        [ReadOnly] public NativeParallelHashSet<int> includeSet;
        [ReadOnly] public NativeParallelHashSet<int> excludeSet;

        public bool IsValid { get; internal set; }

        public int id;

        public bool IsIncludingTiles
        {
            get
            {
                if (!IsValid) return false;
                return includeSet.Count() > 0;
            }
        }
        public bool IsExcludingTiles
        {
            get
            {
                if (!IsValid) return false;
                return excludeSet.Count() > 0;
            }
        }

        public NativeTileMask(List<int> includeList, List<int> excludeList)
        {
            AbstractRandom random = new UnityMathematicsRandom(1);
            id = random.NextInt(100000000);

            IsValid = true;

            includeSet = new NativeParallelHashSet<int>(includeList.Count, Allocator.Persistent);
            excludeSet = new NativeParallelHashSet<int>(excludeList.Count, Allocator.Persistent);

            foreach (var t in includeList)
                includeSet.Add((int)t);

            foreach (var t in excludeList)
                excludeSet.Add((int)t);
        }

        public bool CanModifyTile(Tile tile)
        {
            if (!IsValid) return true;

            bool included = false;
            bool excluded = false;

            foreach (int id in includeSet)
            {
                if (tile.ContainsId(id))
                {
                    included = true;
                    break;
                }
            }

            foreach (int id in excludeSet)
            {
                if (tile.ContainsId(id))
                {
                    excluded = true;
                    break;
                }
            }

            if (IsExcludingTiles && excluded) return false;
            else if (IsIncludingTiles && included) return true;
            else return !IsIncludingTiles;
        }

        public ITileMask DeepClone()
        {
            Allocator allocator = Allocator.Persistent;
            var clone = new NativeTileMask
            {
                includeSet = new(includeSet.Count(), allocator),
                excludeSet = new(excludeSet.Count(), allocator),
                IsValid = this.IsValid
            };

            foreach (var item in includeSet)
                clone.includeSet.Add(item);

            foreach (var item in excludeSet)
                clone.excludeSet.Add(item);

            return clone;
        }

        public void Dispose()
        {
            if (IsValid && includeSet.IsCreated)
            {
                includeSet.Dispose();
                includeSet = default;
            }

            if (IsValid && excludeSet.IsCreated)
            {
                excludeSet.Dispose();
                excludeSet = default;
            }

            IsValid = false;
        }

        public SerialTileMask ToSerialMask()
        {
            var include = new List<int>();
            var exclude = new List<int>();

            if (includeSet.IsCreated)
            {
                using (var keys = includeSet.ToNativeArray(Allocator.Temp))
                {
                    include = new List<int>(keys.Length);
                    // Either a for-loopÅc
                    for (int i = 0; i < keys.Length; i++) include.Add(keys[i]);
                    // Åcor, if you prefer:
                    // include.AddRange(keys.ToArray());
                }
            }

            if (excludeSet.IsCreated)
            {
                using (var keys = excludeSet.ToNativeArray(Allocator.Temp))
                {
                    exclude = new List<int>(keys.Length);
                    for (int i = 0; i < keys.Length; i++) exclude.Add(keys[i]);
                    // exclude.AddRange(keys.ToArray());
                }
            }

            return new SerialTileMask(include, exclude);
        }
    }
}
