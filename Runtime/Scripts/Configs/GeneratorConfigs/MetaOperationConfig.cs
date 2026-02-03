using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class MetaOperationConfig : AbstractGeneratorConfig, IMetaFunctionConfig, IMetaKeyConfig
    {
        public MetaOperationConfig()
        {
            _description = StringType.Description_Generator_MetaOperation;
        }

        public override GeneratorType Type { get { return GeneratorType.MetaOperation; } }

        public MetaFunction MetaFunction { get { return _metaFunction; } set { _metaFunction = value; } }
        [SerializeField] private MetaFunction _metaFunction = default;

        public string MetaKey { get { return _metaFunction.outputKey; } set { _metaFunction.outputKey = value; } }
    }
}