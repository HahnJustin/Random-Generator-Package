
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Dalichrome.RandomGenerator.UserData
{
    [CreateAssetMenu(menuName = "RandomGenerator/UserData/TileLayer")]
    public class TileLayer : AbstractUserData
    {
        [Header("Core Fields")]
        [SerializeField, LayerTypeCollision] public int id;
        public override int GetId() => id;

        public string layerName;
        public string sortingLayerName;
        public bool hasCollider = false;
#if ODIN_INSPECTOR
        [ShowIf("hasCollider", true)]
#endif
        public bool useCompositeCollider = false;
#if ODIN_INSPECTOR && UNITY_EDITOR
        [ValueDropdown(nameof(AllTags))]
#endif
        public string tag = "Untagged";
        public int sortingOrder;
        public int tieOrder;
        public Material material;

#if UNITY_EDITOR
        private static IEnumerable<string> AllTags => UnityEditorInternal.InternalEditorUtility.tags;
#endif
    }
}
