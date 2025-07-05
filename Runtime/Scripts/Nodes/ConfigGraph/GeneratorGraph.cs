using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

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

            // 👇 Step 2: Assign operations + collapse logic filters
            ApplyOperationsAndCollapseFilters(nodeMap[startNode]);

            return nodeMap[startNode];
        }

        private ConfigGraphNode GetOrCreate(Node node, Dictionary<Node, ConfigGraphNode> map, NodeRole role, AbstractConfig config)
        {
            if (!map.TryGetValue(node, out var result))
            {
                result = new ConfigGraphNode(config, role);
                map[node] = result;
            }
            return result;
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
            if (node is AbstractLogicNode) return NodeRole.LogicFilter;
            return NodeRole.NA;
        }

        private void ApplyOperationsAndCollapseFilters(ConfigGraphNode root)
        {
            var queue = new Queue<ConfigGraphNode>();
            var visited = new HashSet<ConfigGraphNode>();

            queue.Enqueue(root);
            visited.Add(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

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
                            .Select(p => (IFilter)p.Operation)
                            .ToList();

                        node.Operation = OperationFactory.CreateLogicFilter((AbstractLogicFilterConfig) node.Config, parentFilters);

                        // Optionally null out collapsed filter ops
                        foreach (var parent in node.Parents)
                        {
                            if (parent.Role == NodeRole.Filter || parent.Role == NodeRole.LogicFilter)
                                parent.Operation = null;
                        }

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
