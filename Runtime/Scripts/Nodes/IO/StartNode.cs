using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CreateNodeMenu("IO/Start")]
    [NodeTint(0.78f, 0.78f, 0.78f)]
    public class StartNode : AbstractNode
    {
        [Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        public TileRegion Output;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != nameof(Output)) return null;
            return null;
        }
    }
}