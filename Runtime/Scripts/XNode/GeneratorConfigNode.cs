// GeneratorConfigNode.cs  (no behaviour change – just new base)
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator;
using XNode;
using UnityEditor;
using UnityEngine;

public class GeneratorConfigNode
        : ConfigNodeBase<AbstractGeneratorConfig>
{
    /* one input, **single** output */
    [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)] public TileRegion Input;

    // “Override” ⇒ when the user drags a second wire on,
    // the previous connection is replaced automatically
    [Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
    public TileRegion Output;

    public override System.Type ConfigBaseType => typeof(AbstractGeneratorConfig);

    public GeneratorType Type => Config?.Type ?? GeneratorType.NA;
    public override int PaletteSeed => (int)Type;

    public override void SyncNameWithType()
    {
        if (string.IsNullOrEmpty(DisplayName))
            DisplayName = Type.ToString().Replace('_', ' ');
    }

    public override object GetValue(NodePort port) =>
        port == GetOutputPort(nameof(Output)) && Enabled
            ? GetInputValue<TileRegion>(nameof(Input))
            : null;
}
