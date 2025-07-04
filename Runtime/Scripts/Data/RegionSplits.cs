using Dalichrome.RandomGenerator;
using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionSplits : AbstractGridOperationData, IEnumerable
{
    private List<RegionBounds> regionBounds = new();


    public RegionSplits(AbstractGridOperationData data) : base(data){}

    public void AddRegion(RegionBounds bounds)
    {
        regionBounds.Add(bounds);
    }

    public void ClearRegions()
    {
        regionBounds.Clear();
    }

    public IEnumerator GetEnumerator()
    {
        return regionBounds.GetEnumerator();
    }

    public void Shuffle()
    {
        regionBounds.Shuffle(Random);
    }
}
