using Dalichrome.RandomGenerator.Core;
using Sirenix.OdinInspector;
using System;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class MetaFunction
    {
#if ODIN_INSPECTOR
        [MultiLineProperty(8)]
        [LabelText("Expression")]
#endif
        public string expression;

#if ODIN_INSPECTOR
        [LabelWidth(80)]
        [LabelText("Output Key")]
#endif
        public string outputKey;
    }
}
