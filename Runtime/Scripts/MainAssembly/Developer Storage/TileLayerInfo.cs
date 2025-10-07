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
    /// </summary>
    public static class TileLayerInfo
    {
        // Adjust if your Resources path differs
        public const string ResourcesPath = "TileLayers";

        // Fallbacks when a layer id is missing
        public const string DefaultSortingLayer = "Default";
        public const string DefaultUnityLayerName = "Default";
        public const string DefaultTag = "Untagged";

        private static readonly object _lock = new();
        private static Dictionary<int, TileLayer> _byId; // id -> TileLayer
        private static int[] _allIds;

        /// <summary>Clear caches (rebuilt on next query).</summary>
        public static void Invalidate()
        {
            lock (_lock)
            {
                _byId = null;
                _allIds = null;
            }
        }

        static TileLayerInfo()
        {
#if UNITY_EDITOR
            // Keep cache fresh while editing in the editor
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
                // Prefer project asset over package default on duplicate IDs.
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
                _allIds = dict.Keys.OrderBy(k => k).ToArray();
            }
        }

        // ---------- Queries ----------

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

        /// Tie-breaker order for layers that share the same sortingOrder.
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
    }
}
