using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Generators;
using System.Collections.Generic;

public class ConfigGraphNode
{
    public AbstractConfig Config;
    public NodeRole Role;
    public IAbstractOperation Operation;
    public List<ConfigGraphNode> Parents = new();
    public List<ConfigGraphNode> Children = new();

    public bool Visited => _visited;
    private bool _visited = false;

    public ConfigGraphNode(AbstractConfig config, NodeRole role)
    {
        Config = config;
        Role = role;
    }

    public AbstractGridOperationData Operate(AbstractGridOperationData data)
    {
        if (_visited) return data;

        AbstractOperationData result = Operation.Do(data);

        switch (Role)
        {
            case NodeRole.Joiner when Operation is IJoiner joiner:
                _visited = joiner.IsReady;
                break;
            case NodeRole.Splitter when Operation is ISplitter splitter:
                _visited = splitter.IsComplete;
                break;
            default:
                _visited = true;
                break;
        }

        return (AbstractGridOperationData)result;
    }
}

