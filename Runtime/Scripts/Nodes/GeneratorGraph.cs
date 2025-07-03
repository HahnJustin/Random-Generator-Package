using Dalichrome.RandomGenerator.Configs;
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
                var config = (current as IConfigNode)?.Config; // null for start/end nodes
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
            else if (node is EndNode) return NodeRole.End;
            else if (node is RegionFilterNode) return NodeRole.Filter;
            else if (node is RegionJoinerNode) return NodeRole.Joiner;
            else if (node is RegionSplitterNode) return NodeRole.Splitter;
            else if (node is GeneratorConfigNode) return NodeRole.Generator;
            else return NodeRole.LogicFilter;
        }
    }
}