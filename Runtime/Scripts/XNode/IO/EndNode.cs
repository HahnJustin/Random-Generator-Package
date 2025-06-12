using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("IO/End")]
[NodeTint(0.78f, 0.78f, 0.78f)]
public class EndNode : AbstractNode
{
    [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
    public TileRegion Input;

    public override object GetValue(NodePort port)
    {
        return null;
    }
}
