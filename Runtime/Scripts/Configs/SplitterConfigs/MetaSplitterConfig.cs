using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class MetaSplitterConfig : AbstractRegionSplitterConfig, IMetaConditionConfig
    {
        public MetaSplitterConfig()
        {
            _description = StringType.Description_Splitter_Meta;
        }

        public override SplitterType Type { get { return SplitterType.Meta; } }

        public MetaCondition MetaCondition { get { return _metaCondition; } set { _metaCondition = value; } }
        [SerializeField] private MetaCondition _metaCondition = default;

    }
}
