using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.Generators;
using System.Collections.Generic;


public class ConfigGraphNode
{
    public AbstractConfig Config;
    public NodeRole Role;
    public IAbstractOperation Operation;
    public List<ConfigGraphNode> Parents = new();
    public List<ConfigGraphNode> Children = new();

    public ConfigGraphNode(AbstractConfig config, NodeRole role)
    {
        Config = config;
        Role = role;
    }
}
