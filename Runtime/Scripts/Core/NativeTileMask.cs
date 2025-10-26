using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Dalichrome.RandomGenerator.Core
{
    public struct NativeTileMask : ITileMask, IDisposable
    {
        [ReadOnly] public NativeParallelHashSet<int> includeSet;
        [ReadOnly] public NativeParallelHashSet<int> includedEmptyZIndices;

        [ReadOnly] public NativeParallelHashSet<int> excludeSet;
        [ReadOnly] public NativeParallelHashSet<int> excludedEmptyZIndices;

        public bool IsValid { get; internal set; }

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

        public NativeTileMask(Allocator allocator)
        {
            includeSet = new(1, allocator);
            includedEmptyZIndices = new(1, allocator);
            excludeSet = new(1, allocator);
            excludedEmptyZIndices = new(1, allocator);
            IsValid = true;
        }

        public NativeTileMask(List<int> includeList, List<int> excludeList, NativeLookupBundle bundle)
        {
            IsValid = true;

            includeSet = new NativeParallelHashSet<int>(includeList.Count, Allocator.Persistent);
            includedEmptyZIndices = new NativeParallelHashSet<int>(includeList.Count, Allocator.Persistent);

            excludeSet = new NativeParallelHashSet<int>(excludeList.Count, Allocator.Persistent);
            excludedEmptyZIndices = new NativeParallelHashSet<int>(excludeList.Count, Allocator.Persistent);

            foreach (var t in includeList)
            {
                includeSet.Add(t);
                if (bundle.tileIdToTileKindLookup[t] == (int)TileKind.Empty)
                    includedEmptyZIndices.Add(bundle.tileIdToLayerIndexLookup[t]);
            }

            foreach (var t in excludeList)
            {
                excludeSet.Add(t);
                if (bundle.tileIdToTileKindLookup[t] == (int)TileKind.Empty)
                    excludedEmptyZIndices.Add(bundle.tileIdToLayerIndexLookup[t]);
            }
        }

        public bool CanModifyTileId(int id)
        {
            if (!IsValid || (!IsExcludingTiles && !IsIncludingTiles)) return true;

            bool included = includeSet.Contains(id);
            bool excluded = excludeSet.Contains(id);

            if (IsExcludingTiles && excluded) return false;
            else if (IsIncludingTiles && included) return true;
            else return !IsIncludingTiles;
        }

        public bool CanModifyColumn(NativeTileColumn column)
        {
            if (!IsValid || (!IsExcludingTiles && !IsIncludingTiles)) return true;

            bool included = false;
            bool excluded = false;

            for (int i = 0; i < column.Length; i++)
            {
                if (includeSet.Contains(column[i]) ||
                   (includedEmptyZIndices.Contains(i) && column[i] == 0))
                {
                    included = true;
                    break;
                }

            }

            for (int i = 0; i < column.Length; i++)
            {
                if (excludeSet.Contains(column[i]) ||
                   (excludedEmptyZIndices.Contains(i) && column[i] == 0))
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
                includeSet = new (includeSet.Count(), allocator),
                includedEmptyZIndices = new (includedEmptyZIndices.Count(), allocator),

                excludeSet = new (excludeSet.Count(), allocator),
                excludedEmptyZIndices = new (excludedEmptyZIndices.Count(), allocator),
                IsValid = this.IsValid
            };

            foreach (var item in includeSet)
                clone.includeSet.Add(item);

            foreach (var item in includedEmptyZIndices)
                clone.includedEmptyZIndices.Add(item);

            foreach (var item in excludeSet)
                clone.excludeSet.Add(item);

            foreach (var item in excludedEmptyZIndices)
                clone.excludedEmptyZIndices.Add(item);

            return clone;
        }

        public void Dispose()
        {
            if (includeSet.IsCreated) { includeSet.Dispose(); includeSet = default; }
            if (includedEmptyZIndices.IsCreated) { includedEmptyZIndices.Dispose(); includedEmptyZIndices = default; }
            if (excludeSet.IsCreated) { excludeSet.Dispose(); excludeSet = default; }
            if (excludedEmptyZIndices.IsCreated) { excludedEmptyZIndices.Dispose(); excludedEmptyZIndices = default; }
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
                    for (int i = 0; i < keys.Length; i++) include.Add(keys[i]);
                }
            }

            if (excludeSet.IsCreated)
            {
                using (var keys = excludeSet.ToNativeArray(Allocator.Temp))
                {
                    exclude = new List<int>(keys.Length);
                    for (int i = 0; i < keys.Length; i++) exclude.Add(keys[i]);
                }
            }

            return new SerialTileMask(include, exclude);
        }
    }
}
