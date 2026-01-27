using Dalichrome.RandomGenerator.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class MetaCondition
    {
#if ODIN_INSPECTOR
        [LabelText("Key Expr")]
        [PropertyOrder(0)]
#endif
        public string keyCondition;

#if ODIN_INSPECTOR
        [PropertyOrder(1)]
        [TableList(AlwaysExpanded = true,
                   DrawScrollView = false,
                   ShowIndexLabels = false)]
        [LabelText("Per-key Conditions")]
#endif
        public List<MetaKeyIntCondition> keyIntConditions = new();
    }
}