using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Dalichrome.RandomGenerator.Core
{
    [Serializable]
    public unsafe struct Tile
    {
        public readonly int x, y;

        private const int layerCount = 4;
        private fixed int layerTypes[layerCount];
        private int _value;

        public int LayerCount { get {return layerCount;} }

        public bool IsValid { get; internal set; }

        public Vector2Int Position => new(x, y);

        public int2 Int2 => new(x, y);

        internal Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
            _value = 0;

            layerTypes[ConvertLayerTypeToIndex(LayerType.Ground)] = (int)TileType.Ground_Light;
            layerTypes[ConvertLayerTypeToIndex(LayerType.Wall)] = (int)TileType.Wall_NA;
            layerTypes[ConvertLayerTypeToIndex(LayerType.Object)] = (int)TileType.Object_NA;
            layerTypes[ConvertLayerTypeToIndex(LayerType.Debug)] = (int)TileType.Debug_NA;

            IsValid = true;
        }

        public int this[LayerType layer]
        {
            get 
            {
                if(layer == LayerType.NA) return (int)TileType.NA;
                return layerTypes[ConvertLayerTypeToIndex(layer)];
            }
            internal set => layerTypes[ConvertLayerTypeToIndex(layer)] = (int)value;
        }

        public int Ground
        {
            get => this[LayerType.Ground];
            private set => this[LayerType.Ground] = value;
        }

        public int Wall
        {
            get => this[LayerType.Wall];
            private set => this[LayerType.Wall] = value;
        }

        public int Object
        {
            get => this[LayerType.Object];
            private set => this[LayerType.Object] = value;
        }

        public int Debug
        {
            get => this[LayerType.Debug];
            private set => this[LayerType.Debug] = value;
        }

        public int Value
        {
            get => _value;
            private set => _value = value;
        }

        private static int ConvertLayerTypeToIndex(LayerType type)
        {
            return (int)type - 1;
        }
        
        internal void Invalidate()
        {
            IsValid = false;
        }

        internal void SetValue(int value)
        {
            if (!IsValid) return;

            _value = value;
        }

        internal void SetLayersByTile(Tile other)
        {
            for (int i = 0; i < layerCount; i++)
            {
                layerTypes[i] = other.layerTypes[i];
            }
        }

        internal void SetId(int id, LayerType layer)
        {
            if (!IsValid) return;

            if (layer == LayerType.NA)
            {
                return;
            }
            else if (id == (int)TileType.Wall_Object_NA)
            {
                Wall = (int)TileType.Wall_NA;
                Object = (int)TileType.Object_NA;
                return;
            }
            else if (id == (int)TileType.NA)
            {
                id = layer switch
                {
                    LayerType.Ground => (int)TileType.Ground_NA,
                    LayerType.Wall => (int)TileType.Wall_NA,
                    LayerType.Object => (int)TileType.Object_NA,
                    LayerType.Debug => (int)TileType.Debug_NA,
                    _ => (int)TileType.NA,
                };
            }

            this[layer] = id;
        }

        public bool ContainsId(int id)
        {
            if (id == (int)TileType.Wall_Object_NA)
                return Wall == (int)TileType.Wall_NA && Object == (int)TileType.Object_NA;

            return Ground == id || Wall == id || Object == id || Debug == id;
        }

        public int GetOccupied() =>
            (Wall == (int)TileType.NA || Wall == (int)TileType.Wall_NA) &&
            (Object == (int)TileType.NA || Object == (int)TileType.Object_NA) ? 0 : 1;

        public int GetOccupied(LayerType layer)
        {
            var type = this[layer];
            return type == (int)TileType.NA ||
                   type == (int)TileType.Ground_NA ||
                   type == (int)TileType.Wall_NA ||
                   type == (int)TileType.Object_NA ||
                   type == (int)TileType.Debug_NA
                   ? 0 : 1;
        }

        public int GetIdInLayer(LayerType layer) => this[layer];
    }
}
