using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Databases;
using Dalichrome.RandomGenerator.UserData;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Linq;
using Unity.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dalichrome.RandomGenerator
{
    public static class TileObjectRegistry
    {
        public const string ResourcesPath = "TileObjects";

        private static readonly object _lock = new();

        private static Dictionary<int, TileObject> _byId;          // id -> TileObject
        private static int[] _allIds;                               // sorted ids
        private static TileObject[] _allTiles;                      // ordered by id
        private static Dictionary<int, List<TileObject>> _byLayer;  // layerId -> list
        private static Dictionary<TileKind, List<TileObject>> _byKind; // kind -> list
        private static Dictionary<int, int> _tileIdToLayerId; // tileId -> layerId
        private static Dictionary<int, int> _tileIdToLayerZ;  // tileId -> z (compressed)
        private static Dictionary<int, int> _tileKindById;   // tileId -> tileKind

        private static readonly Dictionary<int, TileBase> _tileBaseCache = new();
        private static readonly Dictionary<int, TileBase> _numberTileBaseCache = new();

        private static NumberSpriteDatabase _numberSpriteDB;

        public static void SetNumberSpriteDatabase(NumberSpriteDatabase db) => _numberSpriteDB = db;

        public static void Invalidate()
        {
            lock (_lock)
            {
                _byId = null;
                _allIds = null;
                _allTiles = null;
                _byLayer = null;
                _byKind = null;
                _tileIdToLayerId = null;
                _tileIdToLayerZ = null;
                _tileKindById = null;
                _tileBaseCache.Clear();
                _numberTileBaseCache.Clear();
            }
        }

        static TileObjectRegistry()
        {
#if UNITY_EDITOR
            AbstractUserData.AnyChanged += _ => Invalidate();
            Undo.undoRedoPerformed += Invalidate;
            EditorApplication.projectChanged += Invalidate;
            AssemblyReloadEvents.afterAssemblyReload += Invalidate;
#endif
        }

        private static void EnsureBuilt()
        {
            if (_byId != null) return;
            lock (_lock)
            {
                if (_byId != null) return;

                var dict = new Dictionary<int, TileObject>(256);
                var all = Resources.LoadAll<TileObject>(ResourcesPath);

#if UNITY_EDITOR
                // Prefer Assets/ over Packages/ on duplicate IDs
                foreach (var to in all)
                {
                    int id = to.id;
                    if (!dict.TryGetValue(id, out var existing))
                    {
                        dict[id] = to;
                        continue;
                    }

                    string pNew = AssetDatabase.GetAssetPath(to);
                    string pOld = AssetDatabase.GetAssetPath(existing);
                    bool newIsProject = !string.IsNullOrEmpty(pNew) && pNew.StartsWith("Assets/");
                    bool oldIsProject = !string.IsNullOrEmpty(pOld) && pOld.StartsWith("Assets/");
                    if (newIsProject && !oldIsProject)
                        dict[id] = to;
                }
#else
                foreach (var to in all)
                    dict[to.id] = to; // last wins at runtime
#endif

                _byId = dict;
                _allIds = dict.Keys.OrderBy(k => k).ToArray();
                _allTiles = _byId.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

                // Build lightweight indexes
                _byLayer = new Dictionary<int, List<TileObject>>();
                _byKind = new Dictionary<TileKind, List<TileObject>>();

                foreach (var t in _allTiles)
                {
                    // layer index
                    if (!_byLayer.TryGetValue(t.layer, out var listL))
                        _byLayer[t.layer] = listL = new List<TileObject>();
                    listL.Add(t);

                    // kind index
                    if (!_byKind.TryGetValue(t.tileKind, out var listK))
                        _byKind[t.tileKind] = listK = new List<TileObject>();
                    listK.Add(t);
                }

                _tileIdToLayerId = new Dictionary<int, int>(_allTiles.Length);
                _tileIdToLayerZ = new Dictionary<int, int>(_allTiles.Length);

                // Ensure TileLayerRegistry is built so z lookups are ready
                _ = TileLayerRegistry.AllLayerIds; // touches EnsureBuilt()

                foreach (var t in _allTiles)
                {
                    _tileIdToLayerId[t.id] = t.layer;
                    _tileIdToLayerZ[t.id] = TileLayerRegistry.GetLayerZ(t.layer, -1); // -1 if missing
                }

                _tileKindById = new Dictionary<int, int>(_allTiles.Length);
                foreach (var t in _allTiles)
                    _tileKindById[t.id] = (int)t.tileKind;
            }
        }

        // Iteration

        /// <summary>Snapshot of all TileObjects, ordered by id (project overrides applied).</summary>
        public static IReadOnlyList<TileObject> AllTiles
        {
            get { EnsureBuilt(); return _allTiles; }
        }

        /// <summary>Lazy enumerable over all TileObjects (same order as AllTiles).</summary>
        public static IEnumerable<TileObject> EnumerateTiles()
        {
            EnsureBuilt();
            // yield without exposing the backing array for safety
            for (int i = 0; i < _allTiles.Length; i++)
                yield return _allTiles[i];
        }

        /// <summary>Lazy enumerable over all TileObjects (same order as AllTiles).</summary>
        /// <summary>Lazy enumerable of all tile IDs (ascending), same order as AllTileIds.</summary>
        public static IEnumerable<int> EnumerateTileIds()
        {
            EnsureBuilt();
            for (int i = 0; i < _allIds.Length; i++)
                yield return _allIds[i];
        }

        /// <summary>All tiles that target the given layer id.</summary>
        public static IReadOnlyList<TileObject> TilesByLayer(int layerId)
        {
            EnsureBuilt();
            return _byLayer.TryGetValue(layerId, out var list) ? (IReadOnlyList<TileObject>)list : Array.Empty<TileObject>();
        }

        /// <summary>All tiles of a given kind (e.g., TileKind.Empty).</summary>
        public static IReadOnlyList<TileObject> TilesByKind(TileKind kind)
        {
            EnsureBuilt();
            return _byKind.TryGetValue(kind, out var list) ? (IReadOnlyList<TileObject>)list : Array.Empty<TileObject>();
        }

        /// <summary>All known tile ids (sorted ascending).</summary>
        public static IReadOnlyList<int> AllTileIds
        {
            get { EnsureBuilt(); return _allIds; }
        }
        public static IReadOnlyDictionary<int, int> TileIdToLayerId
        {
            get { EnsureBuilt(); return _tileIdToLayerId; }
        }

        public static IReadOnlyDictionary<int, int> TileIdToLayerZ
        {
            get { EnsureBuilt(); return _tileIdToLayerZ; }
        }

        public static IReadOnlyDictionary<int, int> TileKindByTileIdInt
        {
            get { EnsureBuilt(); return _tileKindById; }
        }

        // Getters
        public static bool TryGet(int tileId, out TileObject tile)
        {
            EnsureBuilt();
            return _byId.TryGetValue(tileId, out tile);
        }

        public static string GetTileName(int tileId)
        {
            return TryGet(tileId, out var t)
                ? (string.IsNullOrEmpty(t.tileName) ? $"Tile ID {tileId}" : t.tileName)
                : $"Tile ID {tileId}";
        }

        public static Color GetColor(int tileId, Color fallback = default)
        {
            return TryGet(tileId, out var t) ? t.color : (fallback == default ? Color.white : fallback);
        }

        public static Sprite GetSprite(int tileId)
        {
            return TryGet(tileId, out var t) && t.tileSpawn.spawnType == TileSpawnType.Sprite
                ? t.tileSpawn.sprite
                : null;
        }

        public static Sprite GetMenuSprite(int tileId)
        {
            return TryGet(tileId, out var t) && t.menuSprite != null ? t.menuSprite : t.tileSpawn.sprite;
        }

        public static GameObject GetGameObject(int tileId)
        {
            return TryGet(tileId, out var t) && t.tileSpawn.spawnType == TileSpawnType.GameObject
                ? t.tileSpawn.gameObject
                : null;
        }

        public static TileBase GetTileBase(int tileId)
        {
            if (_tileBaseCache.TryGetValue(tileId, out var tb))
                return tb;

            TileBase tile = null;
            if (TryGet(tileId, out var t) && t.tileSpawn.spawnType == TileSpawnType.TileBase)
            {
                tile = t.tileSpawn.tileBase;
            }

            if (tile == null)
            {
                tile = CreateCustomTileFromSprite(GetSprite(tileId));
            }

            _tileBaseCache[tileId] = tile;
            return tile;
        }
        
        public static TileObject GetTileObject(int tileId)
        {
            EnsureBuilt();
            return _byId.TryGetValue(tileId, out var t) ? t : null;
        }

        public static string GetAssetName(int tileId, string fallback = null)
        {
            EnsureBuilt();
            if (_byId.TryGetValue(tileId, out var t) && t != null)
                return t.name;
            return fallback ?? $"Tile {tileId}";
        }

        public static int GetLayerId(int tileId, int fallback = 0)
        {
            return TryGet(tileId, out var t) ? t.layer : fallback;
        }

        public static TileKind GetTileKind(int tileId, TileKind fallback = TileKind.Normal)
        {
            return TryGet(tileId, out var t) ? t.tileKind : fallback;
        }

        public static bool TryGetLayerIdForTile(int tileId, out int layerId)
        {
            EnsureBuilt();
            return _tileIdToLayerId.TryGetValue(tileId, out layerId);
        }

        public static int GetLayerIdForTile(int tileId, int fallback = 0)
        {
            return TryGetLayerIdForTile(tileId, out var lid) ? lid : fallback;
        }

        public static bool TryGetLayerZForTile(int tileId, out int z)
        {
            EnsureBuilt();
            return _tileIdToLayerZ.TryGetValue(tileId, out z);
        }

        public static int GetLayerZForTile(int tileId, int fallback = -1)
        {
            return TryGetLayerZForTile(tileId, out var z) ? z : fallback;
        }


        // Native Lookups
        public static NativeParallelHashMap<int, int> BuildTileIdToLayerIdNative(Allocator alloc)
            {
                EnsureBuilt();
                var map = new NativeParallelHashMap<int, int>(_tileIdToLayerId.Count, alloc);
                foreach (var kv in _tileIdToLayerId) map.TryAdd(kv.Key, kv.Value);
                return map;
            }

        public static NativeParallelHashMap<int, int> BuildTileIdToLayerZNative(Allocator alloc)
        {
            EnsureBuilt();
            var map = new NativeParallelHashMap<int, int>(_tileIdToLayerZ.Count, alloc);
            foreach (var kv in _tileIdToLayerZ) map.TryAdd(kv.Key, kv.Value);
            return map;
        }


        // Number Tiles - TODO: Move elsewhere
        public static TileBase GetNumberTileBase(int number)
        {
            if (_numberTileBaseCache.TryGetValue(number, out var tb))
                return tb;

            if (_numberSpriteDB == null)
            {
                Debug.LogError("[TileObjectInfo] NumberSpriteDatabase not set. Call SetNumberSpriteDatabase().");
                return null;
            }

            tb = CreateNumberTileBase(number, _numberSpriteDB);
            _numberTileBaseCache[number] = tb;
            return tb;
        }

        // Helpers
        private static TileBase CreateCustomTileFromSprite(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<CustomTileBase>();
            tile.sprite = sprite;
            return tile;
        }

        private static TileBase CreateNumberTileBase(int number, NumberSpriteDatabase db)
        {
            const int W = 16, H = 16;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            // Clear to transparent
            var clear = new Color32[W * H];
            tex.SetPixels32(clear);

            int xOff = 0, yOff = H;

            foreach (char ch in number.ToString())
            {
                var src = db.GetValue(ch)?.GetTexture();
                if (!src) continue;

                int sw = src.width, sh = src.height;
                var srcPixels = src.GetPixels();

                if (xOff + sw >= W)
                {
                    yOff -= sh - 1;
                    xOff = 0;
                    if (yOff - sh < 0) break;
                }
                if (yOff >= H) yOff -= sh;

                for (int y = 0; y < sh; y++)
                    for (int x = 0; x < sw; x++)
                    {
                        int tx = x + xOff, ty = y + yOff;
                        if ((uint)tx >= W || (uint)ty >= H) continue;
                        var pix = srcPixels[y * sw + x];
                        if (pix.a <= 0f) continue;
                        tex.SetPixel(tx, ty, pix);
                    }

                xOff += sw - 1;
            }

            tex.Apply();

            var tile = ScriptableObject.CreateInstance<CustomTileBase>();
            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), W, 0, SpriteMeshType.FullRect);
            tile.sprite = sprite;
            return tile;
        }
    }
}
