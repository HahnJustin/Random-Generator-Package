using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractJoiner<C> : AbstractJoinOperation<C, Generation>, IJoiner
        where C : AbstractRegionJoinerConfig
    {
        protected AbstractJoiner(C config) : base(config) { }

        protected void JoinGenerationOperationMiliseconds(Generation main, List<Generation> toMergeIn)
        {
            foreach (Generation generation in toMergeIn)
            {
                Dictionary<int,long> operationMillis = generation.GetOperationMillisDictionary();
                foreach (KeyValuePair<int, long> pair in operationMillis)
                {
                    if(generation.GetOperationTime(pair.Key) == -1)
                        generation.AddOperationTime(pair.Key, pair.Value);
                }
            }
        }

        protected override void PostEnact(Generation output)
        {
            JoinGenerationOperationMiliseconds(output, components);
        }
    }
}