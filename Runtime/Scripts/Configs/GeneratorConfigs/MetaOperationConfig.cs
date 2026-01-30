using UnityEngine;

namespace Dalichrome.RandomGenerator.Configs
{
    public class MetaOperationConfig : AbstractGeneratorConfig, IMetaFunctionConfig
    {
        public MetaOperationConfig()
        {
            _description = StringType.Description_Generator_MetaOperation;
        }

        public override GeneratorType Type { get { return GeneratorType.MetaOperation; } }

        public MetaFunction MetaFunction { get { return _metaFunction; } set { _metaFunction = value; } }
        [SerializeField] private MetaFunction _metaFunction = default;
    }
}