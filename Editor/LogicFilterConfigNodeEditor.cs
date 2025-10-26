// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CustomNodeEditor(typeof(LogicFilterNode))]
    public class LogicFilterConfigNodeEditor
    : ConfigNodeEditor<LogicFilterNode, AbstractLogicFilterConfig>
    {
        public override int GetWidth() => 250;
    }
}
#endif