using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator.Databases;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;

namespace Dalichrome.RandomGenerator
{
    public class TileInfoGrabber: ITileInfoGrabber
    {
        [SerializeField] private TileInfoDatabase database;
        [SerializeField] private NumberSpriteDatabase numberSpriteDB;

        private static Dictionary<int, TileBase> tileBases = new();
        private static Dictionary<int, TileBase> numberTileBases = new();

        private Dictionary<int, TileObject> tileObjects = new();

        private class ExtractedValue
        {
            public GameObject GameObject { get; }
            public Sprite SpriteValue { get; }
            public Color ColorValue { get; }
            public TileBase TileValue { get; }

            public ExtractedValue(GameObject gameObject, Sprite spriteValue, Color colorValue, TileBase tileValue)
            {
                GameObject = gameObject;
                SpriteValue = spriteValue;
                ColorValue = colorValue;
                TileValue = tileValue;
            }
        }


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
            numberSpriteDB = numberSpriteDatabase;
        }

        public void SetTileObjects(Dictionary<int,TileObject> tileObjects)
        {
            this.tileObjects = tileObjects;
        }

        public GameObject GetGameObject(int id)
        {
            if (id.TryToEnum(out TileType type))
            {
                return GetInfoHelperByType(type, TileInfoType.gameObject).GameObject;
            }
            else if (tileObjects.ContainsKey(id) && tileObjects[id].tileSpawn.spawnType == TileSpawnType.GameObject)
            {
                return tileObjects[id].tileSpawn.gameObject;
            }

            return null;
        }

        public Sprite GetTileSprite(int id)
        {
            if (id.TryToEnum(out TileType type))
            {
                ExtractedValue value = GetInfoHelperByType(type, TileInfoType.sprite);
                if (value == null) return null;

                return value.SpriteValue;
            }
            else if (tileObjects.ContainsKey(id) && tileObjects[id].tileSpawn.spawnType == TileSpawnType.Sprite)
            {
                return tileObjects[id].tileSpawn.sprite;
            }

            return null;
        }

        public Color GetTileColor(int id)
        {
            if (id.TryToEnum(out TileType type))
            {
                return GetInfoHelperByType(type, TileInfoType.color).ColorValue;
            }
            else if (tileObjects.ContainsKey(id))
            {
                return tileObjects[id].color;
            }

            return Color.white;
        }

        public string GetTileName(int id)
        {
            if (id.TryToEnum(out TileType type))
            {
                return type.ToString()
                    .Replace("_", " ")
                    .Replace("-", " ");
            }
            else if (tileObjects.ContainsKey(id))
            {
                return string.IsNullOrEmpty(tileObjects[id].name) ? "Tile ID " + id : tileObjects[id].name;
            }


            return "Tile ID " + id;
        }

        public TileBase GetTileBase(int id)
        {

            if (tileBases.ContainsKey(id))
            {
                return tileBases[id];
            }

            TileBase tile = null;
            if (id.TryToEnum(out TileType type))
            {
                tile = GetInfoHelperByType(type, TileInfoType.tile).TileValue;
            }
            else if (tileObjects.ContainsKey(id) && tileObjects[id].tileSpawn.spawnType == TileSpawnType.TileBase)
            {
                tile = tileObjects[id].tileSpawn.tileBase;
            }

            if (tile != null)
            {
                tileBases[id] = tile;
            }
            else if (!tileBases.ContainsKey(id))
            {
                tileBases[id] = CreateCustomTile(id);
            }
            return tileBases[id];
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

        private ExtractedValue GetInfoHelperByType(TileType type, TileInfoType infoType)
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

        private TileBase CreateCustomTile(int id)
        {
            CustomTileBase tile = (CustomTileBase)ScriptableObject.CreateInstance(typeof(CustomTileBase));
            tile.sprite = GetTileSprite(id);
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
            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 16, 16), new Vector2(0.5f, 0.5f), 16, 0, SpriteMeshType.FullRect);

            tile.sprite = sprite;

            return tile;
        }
    }
}