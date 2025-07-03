using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Nodes
{
    public class ConfigGraphNode
    {
        public AbstractConfig Config;
        public NodeRole Role;
        public List<ConfigGraphNode> Parents = new();
        public List<ConfigGraphNode> Children = new();

        public ConfigGraphNode(AbstractConfig config, NodeRole role)
        {
            Config = config;
            Role = role;
        }
    }
}