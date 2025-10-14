using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class ContainsFilter : AbstractFilter<ContainsFilterConfig>
    {
        public ContainsFilter(ContainsFilterConfig config) : base(config) { }

        public override bool Filter(RegionBounds region)
        {
            int amount = 0;

            foreach (int2 pos in region) 
            { 
                if (TileGrid.ColumnContainsId(pos, config.TileToCheck))
                {
                    amount += 1;
                    if (amount > config.Amount)
                    {
                        return (config.EvaluationMetric == CompareEvaluationType.MoreThanEqual ||
                        config.EvaluationMetric == CompareEvaluationType.AnythingBut);
                    }
                }              
            }

            if (config.EvaluationMetric == CompareEvaluationType.Exactly)
            {
                return amount == config.Amount;
            }
            else if (config.EvaluationMetric == CompareEvaluationType.AnythingBut)
            {
                return amount != config.Amount;
            }
            else if (config.EvaluationMetric == CompareEvaluationType.LessThanEqual)
            {
                return amount <= config.Amount;
            }
            else if (config.EvaluationMetric == CompareEvaluationType.MoreThanEqual)
            {
                return amount >= config.Amount;
            }

            //This should never be reached
            return false;
        }
    }
}
