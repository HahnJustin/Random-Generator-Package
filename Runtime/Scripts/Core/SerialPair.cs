using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    [Serializable]
    public class SerialPair<K, V>
    {
        public K Key;
        public V Value;

        public SerialPair(K key, V value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return $"[{Key}] : {Value}";
        }
    }
}