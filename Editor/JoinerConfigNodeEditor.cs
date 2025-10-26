// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CustomNodeEditor(typeof(RegionJoinerNode))]
    public class JoinerConfigNodeEditor
        : ConfigNodeEditor<RegionJoinerNode, AbstractRegionJoinerConfig>
    {
        public override int GetWidth() => 220;
    }
}
#endif