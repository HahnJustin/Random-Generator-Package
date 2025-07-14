using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public abstract class AbstractRegionFilterConfig : AbstractConfig
    {
        [Hidden] public new virtual FilterType Type { get; }

        public SizeFilterType SizeFilterType { get { return _sizeFilterType; } set { _sizeFilterType = value; } }
        [SerializeField] private SizeFilterType _sizeFilterType;

        public RegionAmountFilterType RegionAmountFilterType { get { return _regionAmountFilterType; } set { _regionAmountFilterType = value; } }
        [SerializeField] private RegionAmountFilterType _regionAmountFilterType;

        [Condition("RegionAmountFilterType", RegionAmountFilterType.Constant)] public int RegionAmount { get { return _regionAmount; } set { _regionAmount = value; } }
        [Condition("RegionAmountFilterType", RegionAmountFilterType.Constant), SerializeField] private int _regionAmount;

        [Condition("RegionAmountFilterType", RegionAmountFilterType.Proportional)] public float RegionProportion { get { return _regionProportion; } set { _regionProportion = value; } }
        [Condition("RegionAmountFilterType", RegionAmountFilterType.Proportional), SerializeField] private float _regionProportion;

        public override string ToString()
        {
            return Type.ToString() + " Filter";
        }
    }
}