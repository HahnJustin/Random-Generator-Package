using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public struct TileMask : IDisposable
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

        public TileMask(List<TileType> includeList, List<TileType> excludeList)
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

            foreach (int type in includeSet)
            {
                if (tile.ContainsType((TileType)type))
                {
                    included = true;
                    break;
                }
            }

            foreach (int type in excludeSet)
            {
                if (tile.ContainsType((TileType)type))
                {
                    excluded = true;
                    break;
                }
            }

            if (IsExcludingTiles && excluded) return false;
            else if (IsIncludingTiles && included) return true;
            else return !IsIncludingTiles;
        }

        public TileMask DeepClone()
        {
            Allocator allocator = Allocator.Persistent;
            var clone = new TileMask
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
    }
}
