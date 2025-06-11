// RegionSplitterNodeEditor.cs
#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

[CustomNodeEditor(typeof(RegionSplitterNode))]
public class SplitterConfigNodeEditor
    : ConfigNodeEditor<RegionSplitterNode, AbstractRegionSplitterConfig>
{
    public override int GetWidth() => 220;

    protected override GUIContent GetHeaderIcon(RegionSplitterNode n) =>
        IconUtils.Find(
            "d_TerrainInspector.TerrainToolSplit",
            "TerrainInspector.TerrainToolSplit",
            "d_AnimatorController Icon",
            "AnimatorController Icon");

}
#endif