// RegionFilterNode.cs
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using System.Linq;
using UnityEngine;
using XNode;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CreateNodeMenu("Flow/Region Filter")]
    [NodeTint(0.33f, 0.28f, 0.33f)]
    public class RegionFilterNode : ConfigNodeBase<AbstractRegionFilterConfig>
    {
        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        public ConditionalRegion Input;

        [Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Strict)]
        public TileRegion Result;

        public override System.Type ConfigBaseType => typeof(AbstractRegionSplitterConfig);

        public override int PaletteSeed =>
        Config != null
            ? Config.GetType().FullName.GetHashCode()
            : 0;

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
}