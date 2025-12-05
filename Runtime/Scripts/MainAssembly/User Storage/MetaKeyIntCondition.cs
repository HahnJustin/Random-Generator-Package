using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MetaKeyIntCondition
{
#if ODIN_INSPECTOR
    [HorizontalGroup("Row", Width = 0.4f)]
    [LabelText("Key")]
#endif
    public string key;

#if ODIN_INSPECTOR
    [HorizontalGroup("Row", Width = 0.6f)]
    [LabelText("Int Condition")]
#endif
    public string intCondition;
}