#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

[CustomNodeEditor(typeof(GeneratorConfigNode))]
public class GeneratorConfigNodeEditor
    : ConfigNodeEditor<GeneratorConfigNode, AbstractGeneratorConfig>
{
    protected override GUIContent GetHeaderIcon(GeneratorConfigNode n) =>
    IconUtils.Find(
        "d_ScriptableObject Icon",        // dark skin
        "ScriptableObject Icon");         // light/old skin
}
#endif