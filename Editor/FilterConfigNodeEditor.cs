// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using System;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CustomNodeEditor(typeof(RegionFilterNode))]
    public class FilterConfigNodeEditor
    : ConfigNodeEditor<RegionFilterNode, AbstractRegionFilterConfig>
    {
        public override int GetWidth() => 250;

        protected override bool IsAllowedType(Type type)
        {
            // DonÅft allow AbstractLogicFilterConfig types here
            return !typeof(AbstractLogicFilterConfig).IsAssignableFrom(type);
        }
    }
}
#endif