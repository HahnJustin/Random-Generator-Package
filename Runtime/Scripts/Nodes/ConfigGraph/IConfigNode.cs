using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Nodes
{
    public interface IConfigNode 
    {
        public AbstractConfig Config { get; }

        public int Priority { get; }
    }
}
