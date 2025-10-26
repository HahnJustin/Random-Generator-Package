using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using NUnit.Framework;
using System;
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

            FinalizeGraph(nodeMap[startNode]);

            return nodeMap[startNode];
        }

        private ConfigGraphNode GetOrCreate(Node node, Dictionary<Node, ConfigGraphNode> map, NodeRole role, AbstractConfig config)
        {
            if (!map.TryGetValue(node, out var configGraphNode))
            {
                configGraphNode = new ConfigGraphNode(config, role);
                map[node] = configGraphNode;
                if (node is IConfigNode iconfigNode)
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
                        node.Operation = OperationFactory.CreateGenerator((AbstractGeneratorConfig)node.Config);
                        break;

                    case NodeRole.Filter:
                        node.Operation = OperationFactory.CreateFilter((AbstractRegionFilterConfig)node.Config);
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

                        node.Operation = OperationFactory.CreateLogicFilter((AbstractLogicFilterConfig)node.Config, parentFilters);

                        HashSet<ConfigGraphNode> newParents = new();
                        HashSet<ConfigGraphNode> oldParents = new();
                        if (node.Parents == null) break;
                        for (int i = node.Parents.Count - 1; i >= 0; i--)
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

        public int GetNodeCount()
        {
            var root = ToConfigGraphRoot();
            if (root == null) return 0;

            int count = 0;
            var visited = new HashSet<ConfigGraphNode>();
            var queue = new Queue<ConfigGraphNode>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;

                if (current.Config != null)
                    count++;

                foreach (var child in current.Children)
                    queue.Enqueue(child);
            }

            return count;
        }

        public List<AbstractConfig> GetConfigList()
        {
            return nodes
                .OfType<IConfigNode>()
                .Select(configNode => configNode.Config)
                .ToList();
        }

        public GeneratorGraph Clone(bool duplicateConfigs = true)
        {
            var newGraph = ScriptableObject.CreateInstance<GeneratorGraph>();
            newGraph.name = (string.IsNullOrEmpty(name) ? nameof(GeneratorGraph) : name) + " [Runtime]";
            newGraph.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideAndDontSave;

            var nodeMap = new Dictionary<Node, Node>(nodes?.Count ?? 0);

            // 1) Clone nodes as new ScriptableObjects
            foreach (var oldNode in nodes)
            {
                if (oldNode == null) continue;
                var newNode = ScriptableObject.CreateInstance(oldNode.GetType()) as Node;
                newNode.graph = newGraph;
                newNode.name = oldNode.name;
                newNode.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.HideAndDontSave;

                // copy serializable fields
                var json = JsonUtility.ToJson(oldNode, false);
                JsonUtility.FromJsonOverwrite(json, newNode);

                // deep-copy config if needed (your configs are [Serializable] classes)
                if (duplicateConfigs && newNode is IConfigNode nCfg && nCfg.Config != null)
                {
                    nCfg.Config = DeepCopyConfig(nCfg.Config); // your method
                }

                newGraph.nodes.Add(newNode); // IMPORTANT: add to the new graph's list
                nodeMap[oldNode] = newNode;
            }

            // 2) Rebuild ports
            foreach (var n in newGraph.nodes) n.UpdatePorts();

            // 3) Reconnect edges (from outputs only)
            foreach (var oldNode in nodes)
            {
                if (!nodeMap.TryGetValue(oldNode, out var src)) continue;

                foreach (var oldPort in oldNode.Ports)
                {
                    if (!oldPort.IsOutput) continue;
                    var newSrcPort = src.GetPort(oldPort.fieldName);
                    if (newSrcPort == null) continue;

                    foreach (var oldConn in oldPort.GetConnections())
                    {
                        if (!nodeMap.TryGetValue(oldConn.node, out var dst)) continue;
                        var newDstPort = dst.GetPort(oldConn.fieldName);
                        if (newDstPort == null) continue;
                        if (!newSrcPort.IsConnectedTo(newDstPort)) newSrcPort.Connect(newDstPort);
                    }
                }
            }

            return newGraph;
        }


        private static AbstractConfig DeepCopyConfig(AbstractConfig source)
        {
            // Serialize full object graph (non-UnityEngine.Object fields get cloned)
            string json = JsonUtility.ToJson(source, false);

            // Create a new instance of the exact runtime type
            var clone = (AbstractConfig)Activator.CreateInstance(source.GetType(), nonPublic: true);

            // Overwrite with data from JSON
            JsonUtility.FromJsonOverwrite(json, clone);

            return clone;
        }
    }
}
