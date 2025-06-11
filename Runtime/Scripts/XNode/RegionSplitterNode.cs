// RegionSplitterNode.cs  (now enjoys the same inspector)
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using System.Linq;
using System;
using XNode;
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator;

[NodeTint(0.38f, 0.28f, 0.28f)]
public class RegionSplitterNode
        : ConfigNodeBase<AbstractRegionSplitterConfig>
{

    /* one source region in … */
    [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)] public TileRegion Source;

    /* … one ConditionalRegion out, connect everywhere you like */
    [Output(typeConstraint = TypeConstraint.Strict)] public ConditionalRegion Region;   // default = Multiple

    public override System.Type ConfigBaseType => typeof(AbstractRegionSplitterConfig);

    public override int PaletteSeed =>
        Config != null
            ? Config.GetType().FullName.GetHashCode()
            : 0;

    public override void SyncNameWithType()
    {
        if (string.IsNullOrEmpty(DisplayName) && Config != null)
            DisplayName = Config.GetType().Name.Replace("Config", "");
    }

    public override object GetValue(NodePort port)
    {
        if (port != GetOutputPort(nameof(Region))) return null;

        // ← Replace this with your real splitting logic
        return new ConditionalRegion();          // same object for every edge
    }
}