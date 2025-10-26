#if UNITY_EDITOR
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Editor;
using UnityEngine;

[CustomNodeEditor(typeof(GeneratorConfigNode))]
public class GeneratorConfigNodeEditor
    : ConfigNodeEditor<GeneratorConfigNode, AbstractGeneratorConfig>
{
}
#endif