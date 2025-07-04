using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CreateNodeMenu("Logic/NOT")]
    public class NotNode : AbstractLogicNode
    {
        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        public TileRegion Input;

        [Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        public TileRegion Output;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != nameof(Output)) return null;
            return null;
        }
    }
}