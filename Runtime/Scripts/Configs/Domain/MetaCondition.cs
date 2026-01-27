using Dalichrome.RandomGenerator.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class MetaCondition
    {
#if ODIN_INSPECTOR
        [PropertyOrder(0)]
        [LabelText("Key Expr")]
#endif
        public string keyCondition;

#if ODIN_INSPECTOR
        [PropertyOrder(1)]
        [LabelText("Conds")]
        [LabelWidth(40)]
        [ListDrawerSettings(
            DraggableItems = false,
            ShowIndexLabels = false,     // <- removes the "Row"/index column
            ShowPaging = false,
            ShowItemCount = false,
            HideAddButton = false,       // set true if you want even tighter
            HideRemoveButton = false,    // set true if you want even tighter
            NumberOfItemsPerPage = 9999
        )]
        // Optional: draw elements inline without the foldout per element
        [InlineProperty]
        [HideReferenceObjectPicker]
#endif
        public List<MetaKeyIntCondition> keyIntConditions = new();
    }
}
