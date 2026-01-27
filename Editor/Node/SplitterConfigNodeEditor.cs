// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

[CustomNodeEditor(typeof(RegionSplitterNode))]
public class SplitterConfigNodeEditor
    : ConfigNodeEditor<RegionSplitterNode, AbstractRegionSplitterConfig>
{
    public override int GetWidth() => 320;

}
#endif