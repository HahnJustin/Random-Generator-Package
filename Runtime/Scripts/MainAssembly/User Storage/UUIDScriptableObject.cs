using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.UserData
{
    public class UUIDScriptableObject : ScriptableObject
    {
        [SerializeField, HideInInspector] private string uuid;

        public string Uuid => uuid;

        /// <summary>Editable label; falls back to asset file name if empty.</summary>
        public string DisplayName => name;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Only run in edit-time and for real assets on disk
            if (!UnityEditor.AssetDatabase.Contains(this))
                return;

            bool changed = false;

            // 1) Ensure we actually have a UUID
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = System.Guid.NewGuid().ToString("N");
                changed = true;
            }

            // 2) Make sure it's unique among all assets of this type
            //    (i.e., all ScriptableObjects whose runtime type == GetType()).
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(myPath) && !string.IsNullOrEmpty(uuid))
            {
                // Search by this concrete type name (e.g. "MyConfigAsset"),
                // which will cover all assets that inherit UUIDScriptableObject via that type.
                string typeFilter = "t:" + GetType().Name;
                string[] guids = UnityEditor.AssetDatabase.FindAssets(typeFilter);

                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (path == myPath)
                        continue; // it's us

                    var other = UnityEditor.AssetDatabase.LoadAssetAtPath<UUIDScriptableObject>(path);
                    if (other == null)
                        continue;

                    if (other.uuid == uuid)
                    {
                        // Collision detected -> re-roll a new ID and break.
                        uuid = System.Guid.NewGuid().ToString("N");
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}