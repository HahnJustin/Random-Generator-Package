using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    public struct MetaData : IDisposable
    {
        private Dictionary<string, int> fieldHashes;

        private NativeParallelHashMap<MetaKey, int> data; // this data errors CS0315: The type 'valueType' cannot be used as type parameter 'T' in the generic type or method 'TypeorMethod<T>'. There is no boxing conversion from 'valueType' to 'referenceType'.
        private NativeParallelMultiHashMap<int3, MetaKey> byPos;
        private NativeParallelMultiHashMap<int, MetaKey> byField;

        private struct MetaKey { public int3 pos; public int field; }

        public MetaData(int val)
        {
            data = new();
            byPos = new();
            byField = new();

            fieldHashes = new();
        }

        private int Hash(string field)
        {
            return 0; //Implement this
        }


        private string Unhash(int fieldHash)
        {
            return ""; //Implement this
        }

        private int GetHash(string field)
        {
            int hash;
            if (!fieldHashes.TryGetValue(field, out hash))
            {
                hash = Hash(field);
                fieldHashes.Add(field, hash);
            }
            return hash;
        }

        private int GetData(MetaKey key)
        {
            return data[key];
        }

        public void AddData(int3 pos, string field, int val)
        {
            int hash = GetHash(field);

            MetaKey key = new()
            {
                pos = pos,
                field = hash
            };

            byPos.Add(pos, key);
            byField.Add(hash, key);
            data.Add(key, val);
        }

        public void AddData(int3 pos, int fieldHash, int val)
        {
            MetaKey key = new()
            {
                pos = pos,
                field = fieldHash
            };

            byPos.Add(pos, key);
            byField.Add(fieldHash, key);
            data.Add(key, val);
        }

        public int GetData(int3 pos, string field)
        {
            int hash;
            if (!fieldHashes.TryGetValue(field, out hash))
            {
                return 0;
            }

            return GetData(pos, hash);
        }

        public int GetData(int3 pos, int fieldHash)
        {
            MetaKey key = new()
            {
                pos = pos,
                field = fieldHash
            };

            return data[key];
        }

        public List<MetaPair> GetAllData(int3 pos)
        {
            List<MetaPair> pairs = new();

            MetaKey key;
            NativeParallelMultiHashMapIterator<int3> it;

            if (byPos.TryGetFirstValue(pos, out key, out it))
            {
                pairs.Add(new MetaPair { field = Unhash(key.field), value = GetData(key) });
                while (byPos.TryGetNextValue(out key, ref it))
                {
                    pairs.Add(new MetaPair { field = Unhash(key.field), value = GetData(key) });
                }
            }
            return pairs;
        }

        public List<PositionValue> GetAllData(int fieldHash)
        {
            List<PositionValue> pairs = new();

            MetaKey key;
            NativeParallelMultiHashMapIterator<int> it;

            if (byField.TryGetFirstValue(fieldHash, out key, out it))
            {
                pairs.Add(new PositionValue { position = key.pos, value = GetData(key) });
                while (byField.TryGetNextValue(out key, ref it))
                {
                    pairs.Add(new PositionValue { position = key.pos, value = GetData(key) });
                }
            }
            return pairs;
        }

        public List<PositionValue> GetAllData(string field)
        {
            int hash = GetHash(field);
            return GetAllData(hash);
        }

        public void Dispose()
        {
            data.Dispose();
            byPos.Dispose();
            byField.Dispose();
        }
    }

    public struct MetaPair
    {
        public string field;
        public int value;
    }

    public struct PositionValue
    {
        public int3 position;
        public int value;
    }
}