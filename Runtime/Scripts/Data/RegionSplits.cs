using Dalichrome.RandomGenerator;
using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Data
{
    public class RegionSplits : AbstractGridOperationData, IEnumerable
    {
        private List<RegionBounds> regionBounds = new();

        public int Count { get { return regionBounds.Count; } }

        public RegionSplits(AbstractGridOperationData data) : base(data) { }

        public void AddRegion(RegionBounds bounds)
        {
            regionBounds.Add(bounds);
        }

        public bool RemoveRegion(RegionBounds bounds)
        {
            return regionBounds.Remove(bounds);
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

        public void Sort(IComparer<RegionBounds> comparer)
        {
            regionBounds.Sort(comparer);
        }
    }
}
