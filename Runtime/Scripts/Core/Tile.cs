using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    [Serializable]
    public struct Tile : IEnumerable
    {
        public readonly int x, y;

        private Dictionary<LayerType, TileType> _types;
        private int _value;

        public bool IsValid { get; internal set; }

        public Vector2Int Position => new(x, y);

        internal Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
            _value = 0;

            _types = new Dictionary<LayerType, TileType>
            {
                { LayerType.Ground, TileType.Ground_Light },
                { LayerType.Wall, TileType.Wall_NA },
                { LayerType.Object, TileType.Object_NA },
                { LayerType.Debug, TileType.Debug_NA }
            };

            IsValid = true;
        }

        private static TileType Sanitize(TileType type, LayerType layer) =>
            TileTypeLayers.GetLayerOfTile(type) == layer
                ? type
                : layer switch
                {
                    LayerType.Ground => TileType.Ground_NA,
                    LayerType.Wall => TileType.Wall_NA,
                    LayerType.Object => TileType.Object_NA,
                    LayerType.Debug => TileType.Debug_NA,
                    _ => TileType.NA,
                };

        public TileType this[LayerType layer]
        {
            get => _types.TryGetValue(layer, out var type) ? type : TileType.NA;
            internal set => _types[layer] = Sanitize(value, layer);
        }

        public TileType Ground
        {
            get => this[LayerType.Ground];
            internal set => this[LayerType.Ground] = value;
        }

        public TileType Wall
        {
            get => this[LayerType.Wall];
            internal set => this[LayerType.Wall] = value;
        }

        public TileType Object
        {
            get => this[LayerType.Object];
            internal set => this[LayerType.Object] = value;
        }

        public TileType Debug
        {
            get => this[LayerType.Debug];
            internal set => this[LayerType.Debug] = value;
        }

        public int Value
        {
            get => _value;
            private set => _value = value;
        }

        internal void Invalidate()
        {
            IsValid = false;
        }

        internal void SetType(TileType type)
        {
            if (!IsValid) return;

            if (type == TileType.Wall_Object_NA)
            {
                Wall = TileType.Wall_NA;
                Object = TileType.Object_NA;
                return;
            }

            this[TileTypeLayers.GetLayerOfTile(type)] = type;
        }

        internal void SetValue(int value)
        {
            if (!IsValid) return;

            _value = value;
        }

        internal void SetTypes(Tile other)
        {
            foreach (TileType type in other)
                SetType(type);
        }

        public bool ContainsType(TileType type)
        {
            if (type == TileType.Wall_Object_NA)
                return Wall == TileType.Wall_NA && Object == TileType.Object_NA;

            return Ground == type || Wall == type || Object == type || Debug == type;
        }

        public int GetOccupied() =>
            (Wall == TileType.NA || Wall == TileType.Wall_NA) &&
            (Object == TileType.NA || Object == TileType.Object_NA) ? 0 : 1;

        public int GetOccupied(LayerType layer)
        {
            var type = this[layer];
            return type == TileType.NA ||
                   type == TileType.Ground_NA ||
                   type == TileType.Wall_NA ||
                   type == TileType.Object_NA ||
                   type == TileType.Debug_NA
                   ? 0 : 1;
        }

        public TileType GetTypeInLayer(LayerType layer) => this[layer];

        public IEnumerator GetEnumerator()
        {
            return new List<TileType> { Ground, Wall, Object, Debug }.GetEnumerator();
        }
    }
}
