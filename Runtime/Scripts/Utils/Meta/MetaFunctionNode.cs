using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Utils
{
    internal abstract class MetaFunctionNode
    {
        internal abstract float Eval(ref TileGrid grid, int x, int y);
    }
}