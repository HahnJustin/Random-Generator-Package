using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Databases
{
    public class Database<T,V> : ScriptableObject
    {
        [SerializeField] private List<SerialPair<T,V>> list;

        private Dictionary<T, V> dict;
        

        public void CreateDictionary()
        {
            if (dict == null)
            {
                dict = new Dictionary<T, V>();
                foreach (SerialPair<T, V> mapping in list)
                {
                    dict.Add(mapping.Key, mapping.Value);
                }
            }
        }

        public V GetValue(T type)
        {
            if (dict == null)
            {
                CreateDictionary();
            }

            if (dict != null && !dict.ContainsKey(type))
            {
                Debug.Log("[Database]: Type to Mapping Dictionary does not contain type:" + type.ToString());
                return default;
            }

            return dict[type];
        }

        public bool ContainsKey(T type)
        {
            if (dict == null)
            {
                CreateDictionary();
                return false;
            }
            else
            {
                return dict.ContainsKey(type);
            }
        }
    }

}