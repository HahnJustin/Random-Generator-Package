using Dalichrome.RandomGenerator.Configs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

[Serializable]
public class LayerInfo
{
    public string layerName;
    public string sortingLayerName;
    public bool hasCollider = false;
#if ODIN_INSPECTOR
    [ShowIf("hasCollider", true)]
#endif
    public bool useCompositeCollider = false;
#if ODIN_INSPECTOR
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