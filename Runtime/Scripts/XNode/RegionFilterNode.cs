// RegionFilterNode.cs
using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Region Filter")]
public class RegionFilterNode : Node
{
    public ConditionalRegion Input;

    [Output] public TileRegion Result;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != nameof(Result)) return null;

        // symbolic rule: return the first connected region
        foreach (var p in DynamicInputs)
        {
            var reg = p.GetInputValue<ConditionalRegion>();
            if (reg != null) return reg;
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate() { UpdatePorts(); }
#endif
}
