using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractJoinOperation<C, D> : AbstractOperation<C, D, D>
        where C : AbstractJoinConfig
        where D : AbstractOperationData
    {
        protected List<D> components = new List<D>();

        protected AbstractJoinOperation(C config) : base(config){ }

        protected override bool RunCondition(D input)
        {
            components.Add(input);
            return components.Count >= config.InputCount;
        }

        protected override D Enact(D input)
        {
            return Join(components); 
        }

        protected abstract D Join(List<D> inputs);
    }
}
