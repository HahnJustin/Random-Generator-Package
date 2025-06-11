// RegionSplitterNode.cs
using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Region Splitter")]
public class RegionSplitterNode : Node
{
    [Input] public TileRegion Source;

    [Output(dynamicPortList = true)]
    public ConditionalRegion[] SubRegions = new ConditionalRegion[0];

    public override object GetValue(NodePort port)
    {
        if (!port.IsOutput) return null;

        if (SubRegions.Length == 0) RunSplit();          // symbolic split

        // Find this port's position among all dynamic outputs
        int idx = DynamicOutputs.ToList().IndexOf(port);
        return idx >= 0 && idx < SubRegions.Length ? SubRegions[idx] : null;
    }

    private void RunSplit()
    {
        var src = GetInputValue<TileRegion>(nameof(Source));
        if (src == null) return;

        // TODO: replace with real partitioner
        SubRegions = new[]
        {
            new ConditionalRegion(),
            new ConditionalRegion(),
            new ConditionalRegion()
        };
    }

#if UNITY_EDITOR
    private void OnValidate() { UpdatePorts(); }
#endif
}
