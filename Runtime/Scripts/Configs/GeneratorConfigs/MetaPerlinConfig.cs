using System;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class MetaPerlinConfig : AbstractGeneratorConfig, IMetaKeyConfig
    {
        public MetaPerlinConfig()
        {
            _description = StringType.Description_Generator_Perlin;
        }

        public override GeneratorType Type { get { return GeneratorType.Perlin; } }

        public string MetaKey { get { return _metaKey; } set { _metaKey = value; } }
        [SerializeField] private string _metaKey = "temperature";

        public float Scale { get { return _scale; } set { _scale = value; } }
        [SerializeField] private float _scale = 5f;

        public bool ScaleWithMapSize { get { return _scaleWithMapSize; } set { _scaleWithMapSize = value; } }
        [SerializeField] private bool _scaleWithMapSize = false;
    }
}