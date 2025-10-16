using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dalichrome.RandomGenerator.UserData;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dalichrome.RandomGenerator
{
    /// <summary>
    /// Static access to TileLayer metadata loaded from Resources/TileLayers.
    /// Project assets override package defaults on duplicate IDs.
    /// Also provides compact Z indices (0..N-1) for active layers.
    /// </summary>
    public static class TileLayerInfo
    {
        public const string ResourcesPath = "TileLayers";

        public const string DefaultSortingLayer = "Default";
        public const string DefaultUnityLayerName = "Default";
        public const string DefaultTag = "Untagged";

        private static readonly object _lock = new();

        private static Dictionary<int, TileLayer> _byId; // layerId -> TileLayer
        private static int[] _allIds;                    // sorted ascending by layerId

        // NEW: compact Z mapping
        private static Dictionary<int, int> _layerIdToZ; // layerId -> z (0..N-1)
        private static int[] _zToLayerId;                // z -> layerId

        private static int[] _zToDefaultLayerOccupance;  // z -> default occupance (0 = false, 1 = true)

        public static void Invalidate()
        {
            lock (_lock)
            {
                _byId = null;
                _allIds = null;
                _layerIdToZ = null;
                _zToLayerId = null;
                _zToDefaultLayerOccupance = null;
            }
        }

        static TileLayerInfo()
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

                var dict = new Dictionary<int, TileLayer>(64);
                var all = Resources.LoadAll<TileLayer>(ResourcesPath);

#if UNITY_EDITOR
                foreach (var tl in all)
                {
                    int id = tl.id;
                    if (!dict.TryGetValue(id, out var existing))
                    {
                        dict[id] = tl;
                        continue;
                    }

                    string pNew = AssetDatabase.GetAssetPath(tl);
                    string pOld = AssetDatabase.GetAssetPath(existing);
                    bool newIsProject = !string.IsNullOrEmpty(pNew) && pNew.StartsWith("Assets/");
                    bool oldIsProject = !string.IsNullOrEmpty(pOld) && pOld.StartsWith("Assets/");
                    if (newIsProject && !oldIsProject)
                        dict[id] = tl;
                }
#else
                foreach (var tl in all)
                    dict[tl.id] = tl; // last wins at runtime
#endif

                _byId = dict;

                // Order by ascending layerId Å® z is the compressed index (0..N-1)
                _allIds = _byId.Keys.OrderBy(k => k).ToArray();

                // Build compact Z maps
                _layerIdToZ = new Dictionary<int, int>(_allIds.Length);
                _zToLayerId = new int[_allIds.Length];
                _zToDefaultLayerOccupance = new int[_allIds.Length];
                for (int z = 0; z < _allIds.Length; z++)
                {
                    int lid = _allIds[z];
                    _layerIdToZ[lid] = z;
                    _zToLayerId[z] = lid;
                }

                // Build Occupance Map
                _zToDefaultLayerOccupance = new int[_allIds.Length];
                for (int z = 0; z < _allIds.Length; z++)
                {
                    _zToDefaultLayerOccupance[z] = _byId[_zToLayerId[z]].occupyOnDefault ? 1 : 0;
                }
            }
        }

        // ---------- Compact Z / index API ----------

        /// <summary>Total active layers (also the depth of your compact grid).</summary>
        public static int LayerCount { get { EnsureBuilt(); return _zToLayerId.Length; } }

        /// <summary>Returns true and sets z if layerId exists; z is in [0, LayerCount-1].</summary>
        public static bool TryGetLayerZ(int layerId, out int z)
        {
            EnsureBuilt();
            return _layerIdToZ.TryGetValue(layerId, out z);
        }

        /// <summary>Gets the compact z for layerId, or fallback (-1 by default) if missing.</summary>
        public static int GetLayerZ(int layerId, int fallback = -1)
        {
            return TryGetLayerZ(layerId, out var z) ? z : fallback;
        }

        /// <summary>Gets the layerId for a compact z; returns -1 if z is out of range.</summary>
        public static int GetLayerIdByZ(int z)
        {
            EnsureBuilt();
            return (uint)z < _zToLayerId.Length ? _zToLayerId[z] : -1;
        }

        /// <summary>Read-only view: layerId Å® z.</summary>
        public static IReadOnlyDictionary<int, int> LayerIdToZ
        {
            get { EnsureBuilt(); return _layerIdToZ; }
        }

        /// <summary>Read-only view: z Å® layerId.</summary>
        public static IReadOnlyList<int> ZToLayerId
        {
            get { EnsureBuilt(); return _zToLayerId; }
        }

        /// <summary>Read-only view: z Å® defaultLayerOccupance ( 1 == true).</summary>
        public static IReadOnlyList<int> ZToDefaultOccupance
        {
            get { EnsureBuilt(); return _zToDefaultLayerOccupance; }
        }

        // ---------- Existing metadata queries ----------

        public static bool TryGet(int layerId, out TileLayer layer)
        {
            EnsureBuilt();
            return _byId.TryGetValue(layerId, out layer);
        }

        public static IReadOnlyList<int> AllLayerIds
        {
            get { EnsureBuilt(); return _allIds; }
        }

        public static int GetSortingOrder(int layerId, int fallback = 0)
        {
            return TryGet(layerId, out var tl) ? tl.sortingOrder : fallback;
        }

        public static int GetSortingLayerID(int layerId)
        {
            if (TryGet(layerId, out var tl) && !string.IsNullOrEmpty(tl.sortingLayerName))
                return SortingLayer.NameToID(tl.sortingLayerName);
            return SortingLayer.NameToID(DefaultSortingLayer);
        }

        /// <summary>Tie-breaker order for equal sortingOrder.</summary>
        public static int GetTieOrder(int layerId, int fallback = 0)
        {
            return TryGet(layerId, out var tl) ? tl.tieOrder : fallback;
        }

        public static int GetUnityLayerID(int layerId)
        {
            string name = DefaultUnityLayerName;
            if (TryGet(layerId, out var tl) && !string.IsNullOrEmpty(tl.layerName))
                name = tl.layerName;

            int id = LayerMask.NameToLayer(name);
#if UNITY_EDITOR
            if (id == -1)
                Debug.LogError($"TileLayer {layerId} refers to Unity Layer '{name}' which does not exist.");
#endif
            return id == -1 ? LayerMask.NameToLayer(DefaultUnityLayerName) : id;
        }

        public static string GetTag(int layerId)
        {
            return TryGet(layerId, out var tl) && !string.IsNullOrEmpty(tl.tag) ? tl.tag : DefaultTag;
        }

        public static bool GetHasCollider(int layerId)
        {
            return TryGet(layerId, out var tl) && tl.hasCollider;
        }

        public static bool GetUseCompositeCollider(int layerId)
        {
            return TryGet(layerId, out var tl) && tl.useCompositeCollider;
        }

        public static Material GetMaterial(int layerId)
        {
            return TryGet(layerId, out var tl) ? tl.material : null;
        }

        public static Sprite GetMenuSprite(int layerId)
        {
            return TryGet(layerId, out var tl) ? tl.menuSprite : null;
        }

        public static string GetName(int layerId)
        {
            return TryGet(layerId, out var tl) ? tl.name : null;
        }
    }
}
