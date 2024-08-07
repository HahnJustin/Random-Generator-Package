using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    [Serializable]
    public class Tile : IEnumerable
    {
        public readonly int x;
        public readonly int y;

        public Vector2Int Vector
        {
            get { return new Vector2Int(x, y); }
        }

        private TileType SanitizeTileType(TileType type, LayerType layer)
        {
            if (TileTypeLayers.GetLayerOfTile(type) == layer)
            {
                return type;
            }

            switch (layer)
            {
                case LayerType.Ground:
                    return TileType.Ground_NA;
                case LayerType.Wall:
                    return TileType.Wall_NA;
                case LayerType.Object:
                    return TileType.Object_NA;
                case LayerType.Debug:
                    return TileType.Debug_NA;
                default:
                    return TileType.NA;
            }
        }

        private TileType _groundType = TileType.Ground_Light;
        public TileType Ground
        {
            get { return _groundType; }
            set { _groundType = SanitizeTileType(value, LayerType.Ground); }
        }

        private TileType _wallType = TileType.Wall_NA;
        public TileType Wall
        {
            get { return _wallType; }
            set { _wallType = SanitizeTileType(value, LayerType.Wall); }
        }

        private TileType _containedObject = TileType.Object_NA;
        public TileType Object
        {
            get { return _containedObject; }
            set { _containedObject = SanitizeTileType(value, LayerType.Object); }
        }

        private TileType _debug = TileType.Debug_NA;
        public TileType Debug
        {
            get { return _debug; }
            set { _debug = SanitizeTileType(value, LayerType.Debug); }
        }

        private int _value = 0;
        public int Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool ContainsType(TileType type)
        {
            if (type == TileType.Wall_Object_NA &&
               Wall == TileType.Wall_NA &&
               Object == TileType.Object_NA)
            {
                return true;
            }

            if (Ground == type) return true;
            if (Wall == type) return true;
            if (Object == type) return true;
            if (Debug == type) return true;

            return false;
        }


        public void SetTypes(Tile toSet)
        {
            foreach (TileType type in toSet)
            {
                SetType(type);
            }
        }

        public void SetType(TileType type)
        {
            if (type == TileType.Wall_Object_NA)
            {
                Wall = TileType.Wall_NA;
                Object = TileType.Object_NA;
                return;
            }

            LayerType layer = TileTypeLayers.GetLayerOfTile(type);
            switch (layer)
            {
                case LayerType.Ground:
                    Ground = type;
                    break;
                case LayerType.Wall:
                    Wall = type;
                    break;
                case LayerType.Object:
                    Object = type;
                    break;
                case LayerType.Debug:
                    Debug = type;
                    break;
                default:
                    break;
            }
        }

        public int GetOccupied()
        {
            if ((Wall == TileType.NA || Wall == TileType.Wall_NA) &&
                (Object == TileType.NA || Object == TileType.Object_NA))
            {
                return 0;
            }
            return 1;
        }

        public int GetOccupied(LayerType layer)
        {
            bool value = false;
            switch (layer)
            {
                case LayerType.Ground:
                    value = Ground != TileType.NA && Ground != TileType.Ground_NA;
                    break;
                case LayerType.Wall:
                    value = Wall != TileType.NA && Wall != TileType.Wall_NA;
                    break;
                case LayerType.Object:
                    value = Object != TileType.NA && Object != TileType.Object_NA;
                    break;
                case LayerType.Debug:
                    value = Debug != TileType.NA && Debug != TileType.Debug_NA;
                    break;
                default:
                    break;
            }
            return value ? 1 : 0;
        }

        public TileType GetTypeInLayer(LayerType layer)
        {
            switch (layer)
            {
                case LayerType.Ground:
                    return Ground;
                case LayerType.Wall:
                    return Wall;
                case LayerType.Object:
                    return Object;
                case LayerType.Debug:
                    return Debug;
                default:
                    return TileType.NA;
            }
        }

        public IEnumerator GetEnumerator()
        {
            List<TileType> types = new() { Ground, Wall, Object, Debug };
            return types.GetEnumerator();
        }
    }
}