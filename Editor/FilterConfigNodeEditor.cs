// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CustomNodeEditor(typeof(RegionFilterNode))]
    public class FilterConfigNodeEditor
    : ConfigNodeEditor<RegionFilterNode, AbstractRegionFilterConfig>
    {
        public override int GetWidth() => 220;

        protected override string IconFilename => "filter-icon";
    }
}
#endif