using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public abstract class AbstractGeneratorConfig : AbstractConfig
    {
        [Hidden] public new virtual GeneratorType Type { get; }

        [Hidden]
        public override string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        //Mask Variables
        [Hidden] public bool Masked { get { return _masked; } set { _masked = value; } }
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
        [SerializeField, ShowIf("Masked", true)] private MaskTimeType _maskTime = MaskTimeType.During;

        [Condition("Masked", true), Color("#4d728f")] public List<TileType> IncludeList { get { return _includeList; } set { _includeList = value; } }
        [SerializeField, ShowIf("Masked", true)] private List<TileType> _includeList = new() {};

        [Condition("Masked", true), Color("#4d728f")] public List<TileType> ExcludeList { get { return _excludeList; } set { _excludeList = value; } }
        [SerializeField, ShowIf("Masked", true)] private List<TileType> _excludeList = new() { TileType.Wall_Object_NA};

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
