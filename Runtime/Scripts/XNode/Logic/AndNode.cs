using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("Logic/AND")]
public class AndNode : AbstractNode
{
    [Input(typeConstraint = TypeConstraint.Strict)]
    public TileRegion Input;

    [Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
    public TileRegion Output;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != nameof(Output)) return null;
        return null;
    }
}