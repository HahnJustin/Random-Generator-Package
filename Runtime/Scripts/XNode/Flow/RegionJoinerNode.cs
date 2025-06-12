// RegionJoinerNode.cs
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Region Joiner")]
[NodeTint(0.28f, 0.28f, 0.38f)]
public class RegionJoinerNode : ConfigNodeBase<AbstractRegionJoinerConfig>
{
    [Input(typeConstraint = TypeConstraint.Strict)]
    public TileRegion Input;

    [Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
    public TileRegion Result;

    public override System.Type ConfigBaseType => typeof(AbstractRegionJoinerConfig);

    public override int PaletteSeed =>
    Config != null
        ? Config.GetType().FullName.GetHashCode()
        : 0;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != nameof(Result)) return null;
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate() { UpdatePorts(); }
#endif
}
