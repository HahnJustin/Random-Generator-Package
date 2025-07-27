using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Dalichrome.RandomGenerator.Nodes
{
    [CreateAssetMenu]
    public class GeneratorGraph : NodeGraph
    {
        public ConfigGraphNode ToConfigGraphRoot()
        {
            Node startNode = FindStartNode();
            if (startNode == null)
            {
                Debug.LogError("No start node found.");
                return null;
            }

            var nodeMap = new Dictionary<Node, ConfigGraphNode>();
            var queue = new Queue<Node>();
            var visited = new HashSet<Node>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();

                var role = GetNodeRole(current);
                var config = (current as IConfigNode)?.Config;
                var currentGraphNode = GetOrCreate(current, nodeMap, role, config);

                foreach (NodePort output in current.Outputs)
                {
                    foreach (var connection in output.GetConnections())
                    {
                        Node target = connection.node;
                        var targetRole = GetNodeRole(target);
                        var targetConfig = (target as IConfigNode)?.Config;
                        var childGraphNode = GetOrCreate(target, nodeMap, targetRole, targetConfig);

                        currentGraphNode.Children.Add(childGraphNode);
                        childGraphNode.Parents.Add(currentGraphNode);

                        if (visited.Add(target))
                            queue.Enqueue(target);
                    }
                }
            }

            FinalizeGraph(nodeMap[startNode]);

            return nodeMap[startNode];
        }

        private ConfigGraphNode GetOrCreate(Node node, Dictionary<Node, ConfigGraphNode> map, NodeRole role, AbstractConfig config)
        {
            if (!map.TryGetValue(node, out var configGraphNode))
            {
                configGraphNode = new ConfigGraphNode(config, role);
                map[node] = configGraphNode;
                if(node is IConfigNode iconfigNode) 
                    configGraphNode.Priority = iconfigNode.Priority;
            }
            return configGraphNode;
        }

        private Node FindStartNode()
        {
            return nodes.FirstOrDefault(n => GetNodeRole(n) == NodeRole.Start);
        }

        private NodeRole GetNodeRole(Node node)
        {
            if (node is StartNode) return NodeRole.Start;
            if (node is EndNode) return NodeRole.End;
            if (node is RegionFilterNode) return NodeRole.Filter;
            if (node is RegionJoinerNode) return NodeRole.Joiner;
            if (node is RegionSplitterNode) return NodeRole.Splitter;
            if (node is GeneratorConfigNode) return NodeRole.Generator;
            if (node is LogicFilterNode) return NodeRole.LogicFilter;
            return NodeRole.NA;
        }

        // Adds Operations to Nodes, Collapses Logic Filters, Orders Node Relatives by Priority
        private void FinalizeGraph(ConfigGraphNode root)
        {
            var queue = new Queue<ConfigGraphNode>();
            var visited = new HashSet<ConfigGraphNode>();

            queue.Enqueue(root);
            visited.Add(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                //Order by Priority
                node.Parents.Sort();
                node.Children.Sort();

                if (node.Operation != null)
                    continue;

                switch (node.Role)
                {
                    case NodeRole.Generator:
                        node.Operation = OperationFactory.CreateGenerator((AbstractGeneratorConfig) node.Config);
                        break;

                    case NodeRole.Filter:
                        node.Operation = OperationFactory.CreateFilter((AbstractRegionFilterConfig) node.Config);
                        break;

                    case NodeRole.Splitter:
                        node.Operation = OperationFactory.CreateSplitter((AbstractRegionSplitterConfig)node.Config, node.Children.Count);
                        break;

                    case NodeRole.Joiner:
                        node.Operation = OperationFactory.CreateJoiner((AbstractRegionJoinerConfig)node.Config, node.Parents.Count);
                        break;

                    case NodeRole.LogicFilter:
                        var parentFilters = node.Parents
                            .Where(p => p.Operation is IFilter)
                            .Select(p => (IFilter) p.Operation)
                            .ToList();

                        node.Operation = OperationFactory.CreateLogicFilter((AbstractLogicFilterConfig) node.Config, parentFilters);

                        HashSet<ConfigGraphNode> newParents = new();
                        HashSet<ConfigGraphNode> oldParents = new();
                        if (node.Parents == null) break;
                        for (int i = node.Parents.Count -1; i >= 0; i--)
                        {
                            var parent = node.Parents[i];
                            if (parent.Role == NodeRole.Filter || parent.Role == NodeRole.LogicFilter)
                            {
                                oldParents.Add(parent);
                                node.Parents.Remove(parent);
                                if (parent.Parents == null) continue;
                                foreach (var grandParent in parent.Parents)
                                {
                                    grandParent.Children.Remove(parent);
                                    newParents.Add(grandParent);
                                }
                            }
                        }

                        foreach (var newParent in newParents)
                        {
                            newParent.Children.Add(node);
                        }

                        node.Parents.AddRange(newParents);
                        break;
                }

                foreach (var child in node.Children)
                {
                    if (visited.Add(child))
                        queue.Enqueue(child);
                }
            }
        }
    }
}
