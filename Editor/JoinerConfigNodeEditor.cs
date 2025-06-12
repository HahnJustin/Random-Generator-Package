// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

[CustomNodeEditor(typeof(RegionJoinerNode))]
public class JoinerConfigNodeEditor
    : ConfigNodeEditor<RegionJoinerNode, AbstractRegionJoinerConfig>
{
    public override int GetWidth() => 220;

    protected override string IconFilename => "joiner-icon.png";
}
#endif