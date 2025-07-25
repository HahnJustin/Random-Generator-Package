using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public abstract class AbstractGeneratorConfig : AbstractConfig
    {
        public override string IconName => "generator-icon";

        [Hidden] public new virtual GeneratorType Type { get; }


        //Mask Variables
        [Hidden]
        public bool Masked
        {
            get
            {
                return _masked && _includeList != null && _excludeList != null && (_includeList.Count > 0 || _excludeList.Count > 0);
            }

            set
            {
                _masked = value;
            }
        }

        public bool TryingMask
        {
            get
            {
                return _masked;
            }

            set
            {
                _masked = value;
            }
        }

        [SerializeField] private bool _masked = false;

        [Hidden]
        public MaskInclusionType MaskInclusion
        {
            get
            {
                int includeCount = _includeList.Count;
                int excludeCount = _excludeList.Count;
                if (includeCount > 0 && excludeCount > 0) return MaskInclusionType.Fusion;
                else if (includeCount > 0) return MaskInclusionType.Inclusive;
                else return MaskInclusionType.Exclusive;
            }
        }

        [Condition("Masked", true), Color("#4d728f")] public MaskTimeType MaskTime { get { return _maskTime; } set { _maskTime = value; } }
        [SerializeField, Condition("TryingMask", true)] private MaskTimeType _maskTime = MaskTimeType.During;

        [Condition("Masked", true), Color("#4d728f"), TileDisplay] public List<int> IncludeList { get { return _includeList; } set { _includeList = value; } }
        [SerializeField, Condition("TryingMask", true), TileDisplay] private List<int> _includeList = new() { };

        [Condition("Masked", true), Color("#4d728f"), TileDisplay] public List<int> ExcludeList { get { return _excludeList; } set { _excludeList = value; } }
        [SerializeField, Condition("TryingMask", true), TileDisplay] private List<int> _excludeList = new() { (int)TileType.Wall_Object_NA };

        public override string ToString()
        {
            return Type.ToString() + " Generator";
        }
    }
}
