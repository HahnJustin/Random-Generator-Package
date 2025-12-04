using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    internal struct MetaData : IDisposable
    {
        private NativeParallelHashMap<MetaKey, int> _data;
        private NativeParallelMultiHashMap<int3, MetaKey> _byPos;
        private NativeParallelMultiHashMap<FixedString64Bytes, MetaKey> _byField;

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
                    IsValid = false
                };
            }

            var clone = new MetaData(_allocator);

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

            keys.Dispose();
            return clone;
        }

        // ---------- Public API: add / update ----------

        public void AddData(int3 pos, string field, int value)
        {
            if (!IsValid) return;
            FixedString64Bytes f = (FixedString64Bytes)field;
            AddData(pos, f, value);
        }

        public void AddData(int3 pos, FixedString64Bytes fixedField, int value)
        {
            if (!IsValid) return;

            EnsureCapacity(1);

            var key = new MetaKey { pos = pos, field = fixedField };

            // Only add to indexes when this is a *new* key
            if (_data.TryAdd(key, value))
            {
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

        public bool TryGetData(int3 pos, string field, out int value)
        {
            value = default;
            if (!IsValid || !_data.IsCreated) return false;

            FixedString64Bytes f = (FixedString64Bytes)field;
            var key = new MetaKey { pos = pos, field = f };
            return _data.TryGetValue(key, out value);
        }

        public bool TryGetData(int3 pos, FixedString64Bytes fixedField, out int value)
        {
            value = default;
            if (!IsValid || !_data.IsCreated) return false;

            var key = new MetaKey { pos = pos, field = fixedField };
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

            var key = new MetaKey { pos = pos, field = (FixedString64Bytes)field };

            if (_data.Remove(key))
            {
                // NOTE: _byPos / _byField still retain stale entries.
                // If you ever need heavy remove usage, we can add a compact step.
                _count = Math.Max(0, _count - 1);
                return true;
            }

            return false;
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

            _capacity = 0;
            _count = 0;
            IsValid = false;
        }
    }
}
