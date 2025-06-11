// RegionJoinerNode.cs
using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Region Joiner")]
public class RegionJoinerNode : Node
{
    [Input(dynamicPortList = true)]
    public TileRegion[] Inputs;

    [Output] public TileRegion Result;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != nameof(Result)) return null;

        // symbolic join: return the first connected region
        foreach (var p in DynamicInputs)
        {
            var reg = p.GetInputValue<TileRegion>();
            if (reg != null) return reg;
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate() { UpdatePorts(); }
#endif
}
