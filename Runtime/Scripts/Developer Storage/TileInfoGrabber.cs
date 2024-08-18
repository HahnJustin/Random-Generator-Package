using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator;
using Dalichrome.RandomGenerator.Databases;

namespace Dalichrome.RandomGenerator
{
    public class TileInfoGrabber: ITileInfoGrabber
    {
        [SerializeField] private TileInfoDatabase database;
        [SerializeField] private NumberSpriteDatabase numberSpriteDB;

        private static Dictionary<TileType, TileBase> tileBases = new();
        private static Dictionary<int, TileBase> numberTileBases = new();

        private record ExtractedValue(GameObject gameObject, Sprite SpriteValue, Color ColorValue, TileBase TileValue);

        private enum TileInfoType
        {
            layer,
            sprite,
            color,
            tile,
            gameObject
        }

        public void SetDatabase(TileInfoDatabase database, NumberSpriteDatabase numberSpriteDatabase = null)
        {
            this.database = database;
            this.numberSpriteDB = numberSpriteDatabase;
        }

        public GameObject GetGameObject(TileType type)
        {
            return GetInfoHelper(type, TileInfoType.gameObject).gameObject;
        }

        public Sprite GetTileSprite(TileType type)
        {
            ExtractedValue value = GetInfoHelper(type, TileInfoType.sprite);
            if (value == null) return null;

            return value.SpriteValue;
        }

        public Color GetTileColor(TileType type)
        {
            return GetInfoHelper(type, TileInfoType.color).ColorValue;
        }

        public TileBase GetTileBase(TileType type)
        {
            if (tileBases.ContainsKey(type))
            {
                return tileBases[type];
            }

            TileBase tile = GetInfoHelper(type, TileInfoType.tile).TileValue;
            if (tile != null)
            {
                tileBases[type] = tile;
            }
            else if (!tileBases.ContainsKey(type))
            {
                tileBases[type] = CreateCustomTile(type);
            }
            return tileBases[type];
        }

        public TileBase GetNumberTileBase(int value)
        {
            if(numberSpriteDB == null)
            {
                Debug.LogError("[TileInfoGrabber] Null NumberSpriteDatabase");
                return default;
            }

            if (numberTileBases.ContainsKey(value))
            {
                return numberTileBases[value];
            }

            numberTileBases[value] = CreateNumberTileBase(value);
            
            return numberTileBases[value];
        }

        private ExtractedValue GetInfoHelper(TileType type, TileInfoType infoType)
        {
            if (database == null)
            {
                Debug.LogError("[TileInfoGrabber] Null Database");
                return default;
            }

            TileInfo tileInfo = database.GetValue(type);

            if (tileInfo == null) return null;

            switch (infoType)
            {
                case TileInfoType.sprite:
                    return new(default, tileInfo.sprite_16, default, default);
                case TileInfoType.color:
                    return new(default, default, tileInfo.color, default);
                case TileInfoType.tile:
                    return new(default, default, default, tileInfo.tile_16);
                case TileInfoType.gameObject:
                    return new(tileInfo.gameObject, default, default, default);
                default:
                    Debug.LogError("[TileInfoGrabber] Bad Tile Info Type Argument");
                    return default;
            }
        }

        private TileBase CreateCustomTile(TileType type)
        {
            CustomTileBase tile = (CustomTileBase)ScriptableObject.CreateInstance(typeof(CustomTileBase));
            tile.sprite = GetTileSprite(type);
            return tile;
        }

        private TileBase CreateNumberTileBase(int number)
        {
            // Create empty texture
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color32[] colors = new Color32[texture.width * texture.height];
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    colors[y * texture.width + x] = new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(colors);

            //Read through each char of number and create texture
            int x_offset = 0;
            int y_offset = texture.height;
            foreach (char num in number.ToString())
            {
                Texture2D source = numberSpriteDB.GetValue(num).GetTexture();
                Color[] sourcePixels = source.GetPixels();
                int sourceWidth = source.width;
                int sourceHeight = source.height;

                if (sourceWidth + x_offset >= texture.width)
                {
                    y_offset -= sourceHeight - 1;
                    x_offset = 0;

                    if (sourceHeight + y_offset < 0)
                    {
                        texture.SetPixel(0, 0, new Color(1, 0, 0, 1));
                        break;
                    }
                }

                if(y_offset >= texture.height)
                {
                    y_offset -= sourceHeight;
                }

                for (int y = 0; y < sourceHeight; y++)
                {
                    for (int x = 0; x < sourceWidth; x++)
                    {
                        Color textPix = texture.GetPixel(x + x_offset, y + y_offset);
                        if (textPix == Color.black) continue;

                        int sourceIndex = y * sourceWidth + x;
                        Color pixel = sourcePixels[sourceIndex];
                        texture.SetPixel(x + x_offset, y + y_offset, pixel);
                    }
                }
                x_offset += sourceWidth - 1;

            }
            
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.Apply();

            CustomTileBase tile = (CustomTileBase)ScriptableObject.CreateInstance(typeof(CustomTileBase));
            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 16, 16), new Vector2(0.5f, 0.5f), 32, 0, SpriteMeshType.FullRect);

            tile.sprite = sprite;

            return tile;
        }
    }
}