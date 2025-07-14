using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Generators;
using System.Collections.Generic;
using System;
using System.Collections;

public class ConfigGraphNode: IComparable<ConfigGraphNode>
{
    public AbstractConfig Config;
    public NodeRole Role;
    public IAbstractOperation Operation;
    public List<ConfigGraphNode> Parents = new();
    public List<ConfigGraphNode> Children = new();
    public int Priority = 10;
    public bool Done => _done || (Config != null && !Config.Enabled);
    private bool _done = false;

    public ConfigGraphNode(AbstractConfig config, NodeRole role)
    {
        Config = config;
        Role = role;
    }

    public AbstractGridOperationData Operate(AbstractGridOperationData data)
    {
        if (Done) return data;

        AbstractOperationData result = Operation.Do(data);

        switch (Role)
        {
            case NodeRole.Joiner when Operation is IJoiner joiner:
                _done = joiner.IsReady;
                break;
            case NodeRole.Splitter when Operation is ISplitter splitter:
                _done = splitter.Done;
                break;
            default:
                _done = true;
                break;
        }

        return (AbstractGridOperationData)result;
    }

    public int CompareTo(ConfigGraphNode node)
    {
        return Priority.CompareTo(node.Priority);
    }
}

