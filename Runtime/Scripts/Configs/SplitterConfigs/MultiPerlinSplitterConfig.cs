using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class MultiPerlinSplitterConfig : AbstractRegionSplitterConfig
    {
        public MultiPerlinSplitterConfig()
        {
            _description = StringType.Description_Splitter_PerlinMulti;
        }

        public override SplitterType Type { get { return SplitterType.MultiPerlin; } }

        public int BucketCount { get { return _bucketCount; } set { _bucketCount = value; } }
        [SerializeField] private int _bucketCount = 4;

        public float FrequencyA { get { return _frequencyA; } set { _frequencyA = value; } }
        [SerializeField] private float _frequencyA = 0.05f;

        public float FrequencyB { get { return _frequencyB; } set { _frequencyB = value; } }
        [SerializeField] private float _frequencyB = 0.08f;

        public float FrequencyC { get { return _frequencyC; } set { _frequencyC = value; } }
        [SerializeField] private float _frequencyC = 0.1f;

        [SerializeField] private Vector3 _fieldWeights = new Vector3(1f, 1f, 1f);
        public Vector3 FieldWeights { get { return _fieldWeights; } set { _fieldWeights = value; } }

        public bool UseRandomOffsets { get { return _useRandomOffsets; } set { _useRandomOffsets = value; } }
        [SerializeField] private bool _useRandomOffsets = true;
    }
}
