using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    internal struct MetaData : IDisposable
    {
        private NativeParallelHashMap<MetaKey, int> _data;
        private NativeParallelMultiHashMap<int3, MetaKey> _byPos;
        private NativeParallelMultiHashMap<FixedString64Bytes, MetaKey> _byField;

        private NativeParallelHashMap<FixedString64Bytes, int> _hotMetaIndices;
        private NativeArray<int> _hotMeta;

        private int _width;
        private int _height;
        private int _hotKeyCount;

        private Allocator _allocator;
        private int _capacity;
        private int _count; // number of unique MetaKey entries

        private const int DefaultInitialCapacity = 4096;

        public static readonly int ColumnZ = -1;

        public bool IsValid { get; internal set; }

        public MetaData(Allocator allocator)
        {
            _allocator = allocator;
            _capacity = DefaultInitialCapacity;
            _count = 0;

            _data = new NativeParallelHashMap<MetaKey, int>(_capacity, allocator);
            _byPos = new NativeParallelMultiHashMap<int3, MetaKey>(_capacity, allocator);
            _byField = new NativeParallelMultiHashMap<FixedString64Bytes, MetaKey>(_capacity, allocator);

            _hotMetaIndices = default;
            _hotMeta = default;

            _width = 0;
            _height = 0;
            _hotKeyCount = 0;

            IsValid = true;
        }

        // ---------- Capacity management ----------

        private void EnsureCapacity(int additionalKeys = 1)
        {
            if (!IsValid) return;

            int needed = _count + additionalKeys;
            if (needed <= _capacity)
                return;

            int newCapacity = _capacity > 0 ? _capacity : 16;

            while (newCapacity < needed)
            {
                if (newCapacity < 524_288) // 512k
                    newCapacity *= 2;      // doubling for small/medium sizes
                else
                    newCapacity += 262_144; // +256k for very large sizes
            }

            Reallocate(newCapacity);
        }

        private void Reallocate(int newCapacity)
        {
            var newData = new NativeParallelHashMap<MetaKey, int>(newCapacity, _allocator);
            var newByPos = new NativeParallelMultiHashMap<int3, MetaKey>(newCapacity, _allocator);
            var newByField = new NativeParallelMultiHashMap<FixedString64Bytes, MetaKey>(newCapacity, _allocator);

            int newCount = 0;

            if (_data.IsCreated)
            {
                var keys = _data.GetKeyArray(Allocator.Persistent);

                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    int value = _data[key];

                    if (newData.TryAdd(key, value))
                    {
                        newByPos.Add(key.pos, key);
                        newByField.Add(key.field, key);
                        newCount++;
                    }
                    else
                    {
                        // Should not happen (MetaKey uniqueness), but if it did,
                        // we still rebuild indexes.
                        newData[key] = value;
                    }
                }

                keys.Dispose();

                _data.Dispose();
                _byPos.Dispose();
                _byField.Dispose();
            }

            _data = newData;
            _byPos = newByPos;
            _byField = newByField;
            _capacity = newCapacity;
            _count = newCount;
        }

        // ---------- Internal helpers ----------

        private bool TryGetData(in MetaKey key, out int value)
        {
            if (!_data.IsCreated)
            {
                value = default;
                return false;
            }

            return _data.TryGetValue(key, out value);
        }

        // ---------- Deep clone ----------

        internal MetaData DeepClone()
        {
            // If this MetaData was never initialized or was disposed, return a non-allocating invalid struct.
            if (!_data.IsCreated || !IsValid)
            {
                return new MetaData
                {
                    _allocator = this._allocator,
                    _capacity = 0,
                    _count = 0,
                    _data = default,
                    _byPos = default,
                    _byField = default,
                    _hotMeta = default,
                    _hotMetaIndices = default,
                    _width = 0,
                    _height = 0,
                    _hotKeyCount = 0,
                    IsValid = false
                };
            }

            var clone = new MetaData(_allocator);

            clone._width = _width;
            clone._height = _height;
            clone._hotKeyCount = _hotKeyCount;

            // Copy all entries from _data/_byPos/_byField
            var keys = _data.GetKeyArray(Allocator.Persistent);
            int needed = keys.Length;

            // Ensure clone has enough capacity to hold all keys in one go
            if (needed > clone._capacity)
                clone.Reallocate(needed);

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                int value = _data[key];

                if (clone._data.TryAdd(key, value))
                {
                    clone._byPos.Add(key.pos, key);
                    clone._byField.Add(key.field, key);
                    clone._count++;
                }
                else
                {
                    clone._data[key] = value;
                }
            }

            if(_hotMetaIndices.IsCreated)
{
                clone._hotMetaIndices = new NativeParallelHashMap<FixedString64Bytes, int>(_hotKeyCount, _allocator);

                // Copy indices
                var hotKeys = _hotMetaIndices.GetKeyArray(Allocator.Persistent);
                for (int i = 0; i < hotKeys.Length; i++)
                {
                    var k = hotKeys[i];
                    clone._hotMetaIndices[k] = _hotMetaIndices[k];
                }
                hotKeys.Dispose();
            }

            if (_hotMeta.IsCreated)
            {
                clone._hotMeta = new NativeArray<int>(_hotMeta.Length, _allocator, NativeArrayOptions.UninitializedMemory);
                NativeArray<int>.Copy(_hotMeta, clone._hotMeta);
            }


            keys.Dispose();
            return clone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHotMetaIndex(int x, int y, int fieldIndex)
        {
            // (((y * width) + x) * hotKeyCount) + fieldIndex
            return ((y * _width) + x) * _hotKeyCount + fieldIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(string key)
        {
            return GetIndex((FixedString64Bytes)key);
        }

        // ---------- Public API: add / update ----------

        public void Initialize(int width, int height, List<FixedString64Bytes> keys)
        {
            if (_hotMeta.IsCreated || _hotMetaIndices.IsCreated)
                throw new InvalidOperationException("MetaData.Initialize() may only be called once.");

            _width = width;
            _height = height;
            _hotKeyCount = keys.Count;

            _hotMetaIndices = new NativeParallelHashMap<FixedString64Bytes, int>(_hotKeyCount, _allocator);
            _hotMeta = new NativeArray<int>(_hotKeyCount * _width * _height, _allocator, NativeArrayOptions.ClearMemory);

            for (int i = 0; i < _hotKeyCount; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_hotMetaIndices.ContainsKey(keys[i]))
                    throw new InvalidOperationException($"Duplicate hot meta key: {keys[i].ToString()}");
#endif
                _hotMetaIndices[keys[i]] = i;
            }
        }

        public int GetIndex(FixedString64Bytes key)
        {
            if (!_hotMetaIndices.IsCreated) return -1;
            return _hotMetaIndices.TryGetValue(key, out int idx) ? idx : -1;
        }

        public void AddData(int3 pos, string field, int value)
        {
            if (!IsValid) return;

            FixedString64Bytes f = (FixedString64Bytes)field;

            if (pos.z == ColumnZ)
            {
                int idx = GetIndex(f);
                if (idx != -1)
                {
                    AddData(new int2(pos.x, pos.y), idx, value);
                    return;
                }
            }
            AddDataHelper(pos, f, value);
        }

        public void AddData(int3 pos, FixedString64Bytes fixedField, int value)
        {
            if (!IsValid) return;

            if (pos.z == ColumnZ)
            {
                int idx = GetIndex(fixedField);
                if (idx != -1)
                {
                    AddData(new int2(pos.x, pos.y), idx, value);
                    return;
                }
            }
            AddDataHelper(pos, fixedField, value);
        }

        public void AddData(int2 pos, int fieldIndex, int value)
        {
            if (!IsValid || !_hotMeta.IsCreated) return;
            if ((uint)pos.x >= (uint)_width || (uint)pos.y >= (uint)_height) return;
            if ((uint)fieldIndex >= (uint)_hotKeyCount) return;

            _hotMeta[GetHotMetaIndex(pos.x, pos.y, fieldIndex)] = value;
        }

        private void AddDataHelper(int3 pos, FixedString64Bytes fixedField, int value)
        {
            var key = new MetaKey(pos, fixedField);

            if (_data.TryAdd(key, value))
            {
                EnsureCapacity(1);
                _byPos.Add(pos, key);
                _byField.Add(fixedField, key);
                _count++;
            }
            else
            {
                _data[key] = value;
            }
        }

        // ---------- Public API: single lookups ----------

        public bool TryGetData(int2 pos, int fieldIndex, out int value)
        {
            value = default;
            if (!IsValid || !_hotMeta.IsCreated) return false;

            if ((uint)pos.x >= (uint)_width || (uint)pos.y >= (uint)_height) return false;
            if ((uint)fieldIndex >= (uint)_hotKeyCount) return false;

            value = _hotMeta[GetHotMetaIndex(pos.x, pos.y, fieldIndex)];
            return true;
        }

        public bool TryGetData(int3 pos, string field, out int value)
        {
            value = default;
            if (!IsValid || !_data.IsCreated) return false;

            FixedString64Bytes f = (FixedString64Bytes)field;

            if (pos.z == ColumnZ)
            {
                int idx = GetIndex(f);
                if (idx != -1)
                {
                    return TryGetData(new int2(pos.x, pos.y), idx, out value);
                }
            }

            var key = new MetaKey(pos, f);
            return _data.TryGetValue(key, out value);
        }

        public bool TryGetData(int3 pos, FixedString64Bytes fixedField, out int value)
        {
            value = default;
            if (!IsValid || !_data.IsCreated) return false;

            if (pos.z == ColumnZ)
            {
                int idx = GetIndex(fixedField);
                if (idx != -1)
                {
                    return TryGetData(new int2(pos.x, pos.y), idx, out value);
                }
            }

            var key = new MetaKey(pos, fixedField);
            return _data.TryGetValue(key, out value);
        }

        // ---------- Public API: group queries ----------

        public List<MetaPair> GetAllData(int3 pos)
        {
            var result = new List<MetaPair>();
            if (!IsValid || !_byPos.IsCreated) return result;

            if (_byPos.TryGetFirstValue(pos, out var key, out var it))
            {
                if (TryGetData(key, out int firstVal))
                {
                    result.Add(new MetaPair
                    {
                        field = key.field.ToString(),
                        value = firstVal
                    });
                }

                while (_byPos.TryGetNextValue(out key, ref it))
                {
                    if (TryGetData(key, out int val))
                    {
                        result.Add(new MetaPair
                        {
                            field = key.field.ToString(),
                            value = val
                        });
                    }
                }
            }

            return result;
        }

        public List<MetaPair> GetAllDataWithColData(int3 pos)
        {
            if (!IsValid || !_byPos.IsCreated)
                return new List<MetaPair>(0);

            // Per-cell data
            List<MetaPair> posData = GetAllData(pos);

            // Column-level data (z = ColumnZ)
            List<MetaPair> colData = GetAllData(new int3(pos.x, pos.y, ColumnZ));

            // Fast paths: if one side is empty, just return the other list directly.
            // NOTE: This reuses the list from GetAllData, which avoids one allocation.
            if (colData.Count == 0)
                return posData;
            if (posData.Count == 0)
                return colData;

            // Both have entries: concatenate.
            // We don't try to dedupe by field for perf reasons; it's OK if duplicates exist.
            var result = new List<MetaPair>(colData.Count + posData.Count);
            result.AddRange(colData);
            result.AddRange(posData);

            return result;
        }

        public List<PositionValue> GetAllData(FixedString64Bytes fixedString)
        {
            var pairs = new List<PositionValue>();
            if (!IsValid || !_byField.IsCreated) return pairs;

            if (_byField.TryGetFirstValue(fixedString, out var key, out var it))
            {
                if (TryGetData(key, out int firstVal))
                {
                    pairs.Add(new PositionValue
                    {
                        position = key.pos,
                        value = firstVal
                    });
                }

                while (_byField.TryGetNextValue(out key, ref it))
                {
                    if (TryGetData(key, out int val))
                    {
                        pairs.Add(new PositionValue
                        {
                            position = key.pos,
                            value = val
                        });
                    }
                }
            }

            return pairs;
        }

        public List<PositionValue> GetAllData(string field)
        {
            return GetAllData((FixedString64Bytes)field);
        }

        public bool Remove(int3 pos, string field)
        {
            if (!IsValid || !_data.IsCreated) return false;

            var key = new MetaKey(pos,(FixedString64Bytes)field);

            if (_data.Remove(key))
            {
                // NOTE: _byPos / _byField still retain stale entries.
                // If you ever need heavy remove usage, we can add a compact step.
                _count = Math.Max(0, _count - 1);
                return true;
            }

            return false;
        }

        public int RemoveAllAt(int3 pos)
        {
            if (!IsValid || !_data.IsCreated || !_byPos.IsCreated) return 0;

            int removed = 0;

            if (_byPos.TryGetFirstValue(pos, out var key, out var it))
            {
                do
                {
                    if (_data.Remove(key))
                    {
                        removed++;
                        _count = Math.Max(0, _count - 1);
                    }
                }
                while (_byPos.TryGetNextValue(out key, ref it));
            }

            // Fully clean the position index. (Does not touch _byField.)
            _byPos.Remove(pos);

            return removed;
        }

        public List<string> GetFields()
        {
            var result = new List<string>();
            if (!IsValid || !_byField.IsCreated)
                return result;

            var keyArray = _byField.GetKeyArray(Allocator.Persistent);
            try
            {
                if (keyArray.Length == 0)
                    return result;

                // Sort in-place so duplicates are adjacent
                keyArray.Sort(); // requires: using Unity.Collections;

                // First element is always included
                FixedString64Bytes last = keyArray[0];
                result.Add(last.ToString());

                // Only add when the key changes
                for (int i = 1; i < keyArray.Length; i++)
                {
                    var current = keyArray[i];
                    if (!current.Equals(last))
                    {
                        result.Add(current.ToString());
                        last = current;
                    }
                }

                return result;
            }
            finally
            {
                keyArray.Dispose();
            }
        }

        // ---------- IDisposable ----------

        public void Dispose()
        {
            if (_data.IsCreated) _data.Dispose();
            if (_byPos.IsCreated) _byPos.Dispose();
            if (_byField.IsCreated) _byField.Dispose();
            if (_hotMeta.IsCreated) _hotMeta.Dispose();
            if (_hotMetaIndices.IsCreated) _hotMetaIndices.Dispose();

            _capacity = 0;
            _count = 0;
            _hotKeyCount = 0;
            _width = 0;
            _height = 0;
            IsValid = false;
        }
    }
}
