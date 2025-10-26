// Editor/IconLoader.cs   (compile-time assembly: Editor)
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Dalichrome.RandomGenerator.EditorHelpers
{

    /// <summary>Loads an icon from either Editor/Icons/ (anywhere) or Unity’s
    /// built-in collection, *without spamming warnings* when the key is missing.</summary>
    public static class IconUtils
    {

        private static readonly Dictionary<string, GUIContent> _cache = new();

        public static GUIContent Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_cache.TryGetValue(key, out var cached)) return cached;

            // 1) try to load a user PNG named key  (key may have or lack ".png")
            var gc = LoadPngFromAnyIconsFolder(key);

            // 2) else try built-in icon name (dark + light)
            gc ??= BuiltInSafe(key);

            // 3) cache (even null so we never probe again)
            _cache[key] = gc;
            return gc;
        }

        // ---------- helpers ----------

        private static GUIContent BuiltInSafe(string name) =>
            EditorGUIUtility.FindTexture(name) != null
                ? EditorGUIUtility.IconContent(name)
                : null;

        private static GUIContent LoadPngFromAnyIconsFolder(string file)
        {
            string fileName = Path.GetFileNameWithoutExtension(file) + ".png";

            // Search once – Editor/Icons/ anywhere in Assets *and* Packages
            string[] guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(file)} t:Texture2D");
            foreach (string g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
                if (path.EndsWith("/Editor/Icons/" + fileName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    return tex ? new GUIContent(tex) : null;
                }
            }
            return null;
        }
    }
}
#endif
