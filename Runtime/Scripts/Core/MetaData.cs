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
        private NativeParallelHashMap<MetaKey, float> _data;
        private NativeParallelMultiHashMap<int3, MetaKey> _byPos;
        private NativeParallelMultiHashMap<FixedString64Bytes, MetaKey> _byField;

        private NativeParallelHashMap<FixedString64Bytes, int> _hotMetaIndices;
        private NativeArray<float> _hotMeta;
        private NativeArray<FixedString64Bytes> _hotKeys;

        private int _width;
        private int _height;
        private int _hotKeyCount;

        private Allocator _allocator;
        private int _capacity;
        private int _count; // number of unique MetaKey entries

        private const int DefaultInitialCapacity = 4096;

        public static readonly int ColumnZ = -1;

        public bool IsValid { get; internal set; }

        private MetaData(Allocator allocator, bool allocateCollections)
        {
            _allocator = allocator;
            _capacity = DefaultInitialCapacity;
            _count = 0;

            _data = allocateCollections ? new NativeParallelHashMap<MetaKey, float>(_capacity, allocator) : default;
            _byPos = allocateCollections ? new NativeParallelMultiHashMap<int3, MetaKey>(_capacity, allocator) : default;
            _byField = allocateCollections ? new NativeParallelMultiHashMap<FixedString64Bytes, MetaKey>(_capacity, allocator) : default;

            // IMPORTANT:
            // Even when "no hot meta", these containers MUST still be constructed
            // (0-sized is fine) so jobs don't complain about unassigned containers.
            _hotMetaIndices = allocateCollections ? new NativeParallelHashMap<FixedString64Bytes, int>(0, allocator) : default;
            _hotKeys = allocateCollections ? new NativeArray<FixedString64Bytes>(0, allocator) : default;
            _hotMeta = allocateCollections ? new NativeArray<float>(0, allocator) : default;

            _width = 0;
            _height = 0;
            _hotKeyCount = 0;

            IsValid = allocateCollections;
        }

        public MetaData(Allocator allocator)
            : this(allocator, true)
        {
        }

        // ---------- Capacity management ----------

        private void EnsureCapacity(int additionalKeys = 1)
        {
            if (!IsValid) return;

            int needed = _count + additionalKeys;
            if (needed <= _capacity) return;

            int newCapacity = _capacity > 0 ? _capacity : 16;
            while (newCapacity < needed)
            {
                if (newCapacity < 524_288) newCapacity *= 2;
                else newCapacity += 262_144;
            }

            Reallocate(newCapacity);
        }

        private void Reallocate(int newCapacity)
        {
            var newData = new NativeParallelHashMap<MetaKey, float>(newCapacity, _allocator);
            var newByPos = new NativeParallelMultiHashMap<int3, MetaKey>(newCapacity, _allocator);
            var newByField = new NativeParallelMultiHashMap<FixedString64Bytes, MetaKey>(newCapacity, _allocator);

            int newCount = 0;

            if (_data.IsCreated)
            {
                var keys = _data.GetKeyArray(Allocator.Temp);

                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    float value = _data[key];

                    if (newData.TryAdd(key, value))
                    {
                        newByPos.Add(key.pos, key);
                        newByField.Add(key.field, key);
                        newCount++;
                    }
                    else
                    {
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

        private bool TryGetData(in MetaKey key, out float value)
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
            // If never constructed (default) or disposed, return default/invalid.
            if (!IsValid || !_data.IsCreated)
                return default;

            // IMPORTANT: false => do not allocate anything we're going to overwrite.
            var clone = new MetaData(_allocator, false);

            // core scalars
            clone._capacity = _capacity;
            clone._count = _count;
            clone._width = _width;
            clone._height = _height;
            clone._hotKeyCount = _hotKeyCount;
            clone.IsValid = true;

            // clone core maps
            int targetCap = math.max(_capacity, 16);
            clone._data = new NativeParallelHashMap<MetaKey, float>(targetCap, _allocator);
            clone._byPos = new NativeParallelMultiHashMap<int3, MetaKey>(targetCap, _allocator);
            clone._byField = new NativeParallelMultiHashMap<FixedString64Bytes, MetaKey>(targetCap, _allocator);

            var keys = _data.GetKeyArray(Allocator.Persistent);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                float val = _data[key];

                if (clone._data.TryAdd(key, val))
                {
                    clone._byPos.Add(key.pos, key);
                    clone._byField.Add(key.field, key);
                }
                else
                {
                    clone._data[key] = val;
                }
            }
            keys.Dispose();

            // clone hot meta (dense: no presence tracking)
            clone._hotMetaIndices = new NativeParallelHashMap<FixedString64Bytes, int>(_hotKeyCount, _allocator);
            clone._hotKeys = new NativeArray<FixedString64Bytes>(_hotKeyCount, _allocator, NativeArrayOptions.UninitializedMemory);
            clone._hotMeta = new NativeArray<float>(_hotMeta.Length, _allocator, NativeArrayOptions.UninitializedMemory);

            // copy hot index map
            var hk = _hotMetaIndices.GetKeyArray(Allocator.Persistent);
            for (int i = 0; i < hk.Length; i++)
            {
                var k = hk[i];
                clone._hotMetaIndices[k] = _hotMetaIndices[k];
            }
            hk.Dispose();

            NativeArray<FixedString64Bytes>.Copy(_hotKeys, clone._hotKeys);
            NativeArray<float>.Copy(_hotMeta, clone._hotMeta);

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
            if (_hotMetaIndices.IsCreated || _hotMeta.IsCreated || _hotKeys.IsCreated)
            {
                if (_hotKeyCount != 0)
                    throw new InvalidOperationException("MetaData.Initialize() may only be called once.");

                // dispose the empty placeholders
                if (_hotMeta.IsCreated) _hotMeta.Dispose();
                if (_hotMetaIndices.IsCreated) _hotMetaIndices.Dispose();
                if (_hotKeys.IsCreated) _hotKeys.Dispose();
            }

            _width = width;
            _height = height;
            _hotKeyCount = keys.Count;

            _hotMetaIndices = new NativeParallelHashMap<FixedString64Bytes, int>(_hotKeyCount, _allocator);
            _hotKeys = new NativeArray<FixedString64Bytes>(_hotKeyCount, _allocator, NativeArrayOptions.UninitializedMemory);

            // Dense: 0 is a valid value and also what you get when "unset".
            // (No presence tracking.)
            _hotMeta = new NativeArray<float>(_hotKeyCount * _width * _height, _allocator, NativeArrayOptions.ClearMemory);

            for (int i = 0; i < _hotKeyCount; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_hotMetaIndices.ContainsKey(keys[i]))
                    throw new InvalidOperationException($"Duplicate hot meta key: {keys[i].ToString()}");
#endif
                _hotMetaIndices[keys[i]] = i;
                _hotKeys[i] = keys[i];
            }
        }

        public int GetIndex(FixedString64Bytes key)
        {
            if (!_hotMetaIndices.IsCreated) return -1;
            return _hotMetaIndices.TryGetValue(key, out int idx) ? idx : -1;
        }

        public void AddData(int3 pos, string field, float value)
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

        public void AddData(int3 pos, FixedString64Bytes fixedField, float value)
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

        public void AddData(int2 pos, int fieldIndex, float value)
        {
            if (!IsValid || !_hotMeta.IsCreated) return;
            if ((uint)pos.x >= (uint)_width || (uint)pos.y >= (uint)_height) return;
            if ((uint)fieldIndex >= (uint)_hotKeyCount) return;

            _hotMeta[GetHotMetaIndex(pos.x, pos.y, fieldIndex)] = value;
        }

        private void AddDataHelper(int3 pos, FixedString64Bytes fixedField, float value)
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

        public bool TryGetData(int2 pos, int fieldIndex, out float value)
        {
            value = default;
            if (!IsValid || !_hotMeta.IsCreated) return false;

            if ((uint)pos.x >= (uint)_width || (uint)pos.y >= (uint)_height) return false;
            if ((uint)fieldIndex >= (uint)_hotKeyCount) return false;

            value = _hotMeta[GetHotMetaIndex(pos.x, pos.y, fieldIndex)];
            return true;
        }

        public bool TryGetData(int3 pos, string field, out float value)
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

        public bool TryGetData(int3 pos, FixedString64Bytes fixedField, out float value)
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
        // Dense: no "presence", so these return ALL hot keys for a tile (including 0 values).

        public List<MetaPair> GetAllData(int3 pos)
        {
            var result = new List<MetaPair>();
            if (!IsValid) return result;

            // 1) Hot meta for ColumnZ
            if (pos.z == ColumnZ && _hotMeta.IsCreated && _hotKeys.IsCreated)
            {
                int2 p2 = new int2(pos.x, pos.y);

                if ((uint)p2.x < (uint)_width && (uint)p2.y < (uint)_height)
                {
                    for (int i = 0; i < _hotKeyCount; i++)
                    {
                        float v = _hotMeta[GetHotMetaIndex(p2.x, p2.y, i)];
                        result.Add(new MetaPair
                        {
                            field = _hotKeys[i].ToString(),
                            value = v
                        });
                    }
                }
            }

            // 2) Hash-map meta (per-pos, and any non-hot ColumnZ entries)
            if (_byPos.IsCreated && _data.IsCreated)
            {
                if (_byPos.TryGetFirstValue(pos, out var key, out var it))
                {
                    do
                    {
                        if (TryGetData(key, out float val))
                        {
                            result.Add(new MetaPair
                            {
                                field = key.field.ToString(),
                                value = val
                            });
                        }
                    }
                    while (_byPos.TryGetNextValue(out key, ref it));
                }
            }

            return result;
        }

        public List<MetaPair> GetAllDataWithColData(int3 pos)
        {
            if (!IsValid) return new List<MetaPair>(0);
            if (pos.z == ColumnZ) return GetAllData(pos);

            var colData = GetAllData(new int3(pos.x, pos.y, ColumnZ));
            var posData = GetAllData(pos);

            if (colData.Count == 0) return posData;
            if (posData.Count == 0) return colData;

            colData.AddRange(posData);
            return colData;
        }

        public List<PositionValue> GetAllData(FixedString64Bytes fixedString)
        {
            var pairs = new List<PositionValue>();
            if (!IsValid) return pairs;

            // Hot meta path (ColumnZ only)
            int hotIdx = GetIndex(fixedString);
            if (hotIdx != -1 && _hotMeta.IsCreated)
            {
                for (int y = 0; y < _height; y++)
                    for (int x = 0; x < _width; x++)
                    {
                        float v = _hotMeta[GetHotMetaIndex(x, y, hotIdx)];
                        pairs.Add(new PositionValue
                        {
                            position = new int3(x, y, ColumnZ),
                            value = v
                        });
                    }
            }

            // Hashed meta path
            if (_byField.IsCreated && _data.IsCreated)
            {
                if (_byField.TryGetFirstValue(fixedString, out var key, out var it))
                {
                    do
                    {
                        if (TryGetData(key, out float val))
                        {
                            pairs.Add(new PositionValue { position = key.pos, value = val });
                        }
                    }
                    while (_byField.TryGetNextValue(out key, ref it));
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

            var key = new MetaKey(pos, (FixedString64Bytes)field);

            if (_data.Remove(key))
            {
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
            if (!IsValid)
                return result;

            // Add hot keys first (unique)
            if (_hotKeys.IsCreated)
            {
                for (int i = 0; i < _hotKeys.Length; i++)
                    result.Add(_hotKeys[i].ToString());
            }

            // Add hashed keys
            if (!_byField.IsCreated)
                return result;

            var keyArray = _byField.GetKeyArray(Allocator.Persistent);
            try
            {
                if (keyArray.Length > 0)
                {
                    keyArray.Sort();

                    FixedString64Bytes last = keyArray[0];
                    result.Add(last.ToString());

                    for (int i = 1; i < keyArray.Length; i++)
                    {
                        var current = keyArray[i];
                        if (!current.Equals(last))
                        {
                            result.Add(current.ToString());
                            last = current;
                        }
                    }
                }
            }
            finally
            {
                keyArray.Dispose();
            }

            return result.Distinct().ToList();
        }

        // ---------- IDisposable ----------

        public void Dispose()
        {
            if (_data.IsCreated) _data.Dispose();
            if (_byPos.IsCreated) _byPos.Dispose();
            if (_byField.IsCreated) _byField.Dispose();

            if (_hotMeta.IsCreated) _hotMeta.Dispose();
            if (_hotMetaIndices.IsCreated) _hotMetaIndices.Dispose();
            if (_hotKeys.IsCreated) _hotKeys.Dispose();

            _capacity = 0;
            _count = 0;
            _hotKeyCount = 0;
            _width = 0;
            _height = 0;
            IsValid = false;
        }
    }
}
