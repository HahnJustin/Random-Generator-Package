using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Core;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractFilter<C>: AbstractGridOperation<C, RegionSplits, Generation>, IFilter
        where C : AbstractRegionFilterConfig
    {
        protected AbstractFilter(C config) : base(config) {}

        public abstract bool Filter(RegionBounds bounds);

        private void Sort(RegionSplits regionSplits)
        {
            if (config.SizeFilterType == SizeFilterType.Largest)
            {
                regionSplits.Sort(RegionBounds.SizeComparerDescending);
            }
            else if (config.SizeFilterType == SizeFilterType.Smallest)
            {
                regionSplits.Sort(RegionBounds.SizeComparerAscending);
            }
            else if (config.SizeFilterType == SizeFilterType.Random)
            {
                regionSplits.Shuffle();
            }
        }

        public virtual void SubInitialize(RegionSplits splits)
        {
            InitializeByInput(splits);
            Initialize(splits);
            InitializeUtils();
        }

        protected override void Initialize(RegionSplits splits)
        {
            TileGrid = splits.Grid;
            SetUtilsTileGrid();
        }

        protected override void InitializeUtils()
        {
            if (TileGrid != null)
                base.InitializeUtils();
        }

        protected override Generation Enact(RegionSplits regionSplits)
        {
            Sort(regionSplits);
            List<RegionBounds> chosenRegions = new();
            foreach (RegionBounds region in regionSplits)
            {
                if (Filter(region))
                {
                    chosenRegions.Add(region);
                    if (config.RegionAmountFilterType == RegionAmountFilterType.Single ||
                        (config.RegionAmountFilterType == RegionAmountFilterType.Constant && chosenRegions.Count >= config.RegionAmount) ||
                        (config.RegionAmountFilterType == RegionAmountFilterType.Proportional && chosenRegions.Count / (float)regionSplits.Count > config.RegionProportion)) 
                        break;
                }
            }

            if (chosenRegions.Count >= 1)
            {
                Generation generation = new(regionSplits);
                RegionBounds mainRegion = chosenRegions[0];

                chosenRegions.RemoveAt(0);
                regionSplits.RemoveRegion(mainRegion);

                foreach (RegionBounds regionBounds in chosenRegions)
                {
                    regionSplits.RemoveRegion(regionBounds);
                    mainRegion.AddRegion(regionBounds);
                }

                generation.Grid.SetRegionBounds(mainRegion);
                return generation;
            }

            return new Generation();
        }
    }
}