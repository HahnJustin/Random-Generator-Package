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
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }

}