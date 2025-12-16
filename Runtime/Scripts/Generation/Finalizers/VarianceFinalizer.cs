using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class VarianceFinalizer : AbstractFinalizer
    {
        protected override bool RunCondition()
        {
            return bundle.tileIdToVarianceFinalizer.IsCreated && !bundle.tileIdToVarianceFinalizer.IsEmpty;
        }

        protected override Generation Do(Generation generation)
        {
            foreach (int3 position in tileGrid.GetPositions3D())
            {
                int tileId = tileGrid.GetTileIdWithLayerIndex(position);
                if (bundle.tileIdToVarianceFinalizer.Contains(tileId))
                {
                    tileGrid.AddData(position, MetaKeyType.VARIANCE, random.NextInt(1000));
                }
            }

            return generation;
        }
    }
}