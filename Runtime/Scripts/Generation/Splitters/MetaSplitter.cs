using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Utils;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class MetaSplitter : AbstractSplitter<MetaSplitterConfig>
    {
        private FloodFillUtil floodFillUtil;
        private MetaConditionUtil conditionUtil;

        public MetaSplitter(MetaSplitterConfig config, int outputs) : base(config, outputs) 
        {
            floodFillUtil = new(config);
            AddUtil(floodFillUtil);
            conditionUtil = new(config);
            AddUtil(conditionUtil);
        }

        protected override RegionSplits Split(Generation generation)
        {
            RegionBounds bounds = generation.Grid.GetRegionBounds();
            RegionSplits regionSplits = new(generation);

            floodFillUtil.SetFillPredicate(pos => conditionUtil.MatchMetaCondition(generation.Grid.GetAllDataWithColData(pos)));

            int left = int.MaxValue;
            int bottom = int.MaxValue;
            int top = int.MinValue;
            int right = int.MinValue;

            floodFillUtil.ResetVisited();

            foreach (int2 pos in generation.Grid.GetPositions())
            {
                if (floodFillUtil.IsVisited(pos) || generation.Grid.IsRestricted(pos)) continue;

                left = int.MaxValue;
                bottom = int.MaxValue;
                right = int.MinValue;
                top = int.MinValue;

                var positions = new List<int2>();

                floodFillUtil.SetOnFillAction(p =>
                {
                    positions.Add(p);
                    if (p.x < left) left = p.x;
                    if (p.y < bottom) bottom = p.y;
                    if (p.x > right) right = p.x;
                    if (p.y > top) top = p.y;
                });

                floodFillUtil.FloodFill(pos);

                if (positions.Count == 0) continue;

                regionSplits.AddRegion(new RegionBounds(
                    new int2(left, bottom),
                    new int2(right, top),
                    positions,
                    generation.Grid
                ));
            }

            return regionSplits;
        }
    }
}