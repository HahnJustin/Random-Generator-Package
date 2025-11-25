using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    internal struct MetaData : IDisposable
    {
        private Dictionary<string, ulong> _fieldToHash;
        private Dictionary<ulong, string> _hashToField;

        private NativeParallelHashMap<MetaKey, int> _data;
        private NativeParallelMultiHashMap<int3, MetaKey> _byPos;
        private NativeParallelMultiHashMap<ulong, MetaKey> _byField;

        private Allocator _allocator;
        private int _capacity;
        private int _count; // number of unique MetaKey entries

        private const int DefaultInitialCapacity = 4096;

        public static readonly int ColumnZ = -1;

        public MetaData(Allocator allocator)
        {
            _allocator = allocator;

            _fieldToHash = new Dictionary<string, ulong>(StringComparer.Ordinal);
            _hashToField = new Dictionary<ulong, string>();

            _capacity = DefaultInitialCapacity;
            _count = 0;

            _data = new NativeParallelHashMap<MetaKey, int>(_capacity, allocator);
            _byPos = new NativeParallelMultiHashMap<int3, MetaKey>(_capacity, allocator);
            _byField = new NativeParallelMultiHashMap<ulong, MetaKey>(_capacity, allocator);
        }

        // ---------- Hashing / field registry ----------

        // 64-bit FNV-1a
        private static ulong Hash(string field)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;

                ulong h = offset;
                for (int i = 0; i < field.Length; i++)
                {
                    h ^= field[i];
                    h *= prime;
                }
                return h;
            }
        }

        private ulong GetHash(string field)
        {
            if (_fieldToHash.TryGetValue(field, out var existing))
                return existing;

            ulong hash = Hash(field);

            // Collision detection: should basically never happen with 64-bit,
            // but if it does, fail loudly.
            if (_hashToField.TryGetValue(hash, out var existingName) && existingName != field)
            {
                throw new InvalidOperationException(
                    $"MetaData hash collision between '{existingName}' and '{field}'. " +
                    "Rename one of the fields or change the hash scheme."
                );
            }

            _fieldToHash[field] = hash;
            _hashToField[hash] = field;
            return hash;
        }

        private string Unhash(ulong fieldHash)
        {
            if (_hashToField.TryGetValue(fieldHash, out var name))
                return name;

            return $"#field_{fieldHash}";
        }

        // ---------- Capacity management ----------

        private void EnsureCapacity(int additionalKeys = 1)
        {
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
            var newByField = new NativeParallelMultiHashMap<ulong, MetaKey>(newCapacity, _allocator);

            int newCount = 0;

            if (_data.IsCreated)
            {
                var keys = _data.GetKeyArray(Allocator.Temp);

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
                        // Should not happen (MetaKey uniqueness), but if it did, we still rebuild indexes.
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
            => _data.TryGetValue(key, out value);

        // ---------- Deep clone ----------

        internal MetaData DeepClone(Allocator allocator)
        {
            // If this MetaData was never initialized, just return an empty one
            if (!_data.IsCreated)
                return new MetaData(allocator);

            var clone = new MetaData(allocator);

            // Copy dictionaries (string <-> hash)
            foreach (var kv in _fieldToHash)
                clone._fieldToHash.Add(kv.Key, kv.Value);

            foreach (var kv in _hashToField)
                clone._hashToField.Add(kv.Key, kv.Value);

            // Copy all entries from _data/_byPos/_byField
            var keys = _data.GetKeyArray(Allocator.Temp);
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
            ulong hash = GetHash(field);
            AddData(pos, hash, value);
        }

        public void AddData(int3 pos, ulong fieldHash, int value)
        {
            EnsureCapacity(1);

            var key = new MetaKey { pos = pos, field = fieldHash };

            if (_data.TryAdd(key, value))
            {
                _byPos.Add(pos, key);
                _byField.Add(fieldHash, key);
                _count++; // new unique key
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
            if (!_fieldToHash.TryGetValue(field, out var hash))
                return false;

            return TryGetData(pos, hash, out value);
        }

        public bool TryGetData(int3 pos, ulong fieldHash, out int value)
        {
            var key = new MetaKey { pos = pos, field = fieldHash };
            return _data.TryGetValue(key, out value);
        }

        // ---------- Public API: group queries ----------

        public List<MetaPair> GetAllData(int3 pos)
        {
            var pairs = new List<MetaPair>();

            if (_byPos.TryGetFirstValue(pos, out var key, out var it))
            {
                if (TryGetData(key, out int firstVal))
                {
                    pairs.Add(new MetaPair
                    {
                        field = Unhash(key.field),
                        value = firstVal
                    });
                }

                while (_byPos.TryGetNextValue(out key, ref it))
                {
                    if (TryGetData(key, out int val))
                    {
                        pairs.Add(new MetaPair
                        {
                            field = Unhash(key.field),
                            value = val
                        });
                    }
                }
            }

            return pairs;
        }

        public List<PositionValue> GetAllData(ulong fieldHash)
        {
            var pairs = new List<PositionValue>();

            if (_byField.TryGetFirstValue(fieldHash, out var key, out var it))
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
            if (!_fieldToHash.TryGetValue(field, out var hash))
                return new List<PositionValue>();

            return GetAllData(hash);
        }

        public bool Remove(int3 pos, string field)
        {
            if (!_fieldToHash.TryGetValue(field, out var hash))
                return false;

            var key = new MetaKey { pos = pos, field = hash };

            if (_data.Remove(key))
            {
                _count = Math.Max(0, _count - 1);
                return true;
            }

            return false;
        }

        public List<string> GetFields()
        {
            return _fieldToHash.Keys.ToList();
        }

        // ---------- IDisposable ----------

        public void Dispose()
        {
            if (_data.IsCreated) _data.Dispose();
            if (_byPos.IsCreated) _byPos.Dispose();
            if (_byField.IsCreated) _byField.Dispose();

            _capacity = 0;
            _count = 0;
        }
    }
}
