using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Dalichrome.RandomGenerator
{
    [CreateAssetMenu]
    public class GeneratorGraph : NodeGraph
    {
        private ConfigGraphNode _cachedRoot;
        [NonSerialized] private bool _cacheValid;
        [NonSerialized] private List<AbstractConfig> _cachedConfigsOnPath;

        public ConfigGraphNode ToConfigGraphRoot()
        {
            // Use cache if already built
            //if (_cacheValid && _cachedRoot != null)
            //    return _cachedRoot;

            Node startNode = FindStartNode();
            if (startNode == null)
            {
                Debug.LogError("No start node found.");
                _cacheValid = false;
                _cachedRoot = null;
                _cachedConfigsOnPath = new List<AbstractConfig>(0);
                return null;
            }

            // ------------------------------------------------------------
            // 1) Compute nodes reachable from Start (topology only)
            // ------------------------------------------------------------
            var reachable = new HashSet<Node>();
            var q = new Queue<Node>();
            q.Enqueue(startNode);
            reachable.Add(startNode);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                foreach (var nxt in Outgoing(cur))
                {
                    if (nxt == null) continue;
                    if (reachable.Add(nxt))
                        q.Enqueue(nxt);
                }
            }

            // ------------------------------------------------------------
            // 2) Compute nodes that can reach an End (reverse BFS, topology only)
            // ------------------------------------------------------------
            var rev = BuildReverseAdjacency(reachable);

            var canReachEnd = new HashSet<Node>();
            var rq = new Queue<Node>();

            foreach (var n in reachable)
            {
                if (GetNodeRole(n) == NodeRole.End)
                {
                    canReachEnd.Add(n);
                    rq.Enqueue(n);
                }
            }

            while (rq.Count > 0)
            {
                var cur = rq.Dequeue();
                if (!rev.TryGetValue(cur, out var parents)) continue;

                for (int i = 0; i < parents.Count; i++)
                {
                    var p = parents[i];
                    if (p == null) continue;

                    if (canReachEnd.Add(p))
                        rq.Enqueue(p);
                }
            }

            // Intersection = nodes on some Start->...->End path
            var onPath = new HashSet<Node>();
            foreach (var n in reachable)
                if (canReachEnd.Contains(n))
                    onPath.Add(n);

            if (!onPath.Contains(startNode))
            {
                Debug.LogError("Start node is not on a Start->End path (no valid End reachable).");
                _cacheValid = false;
                _cachedRoot = null;
                _cachedConfigsOnPath = new List<AbstractConfig>(0);
                return null;
            }

            // ------------------------------------------------------------
            // 3) Build runtime ConfigGraph:
            //    - Materialize Start/End always
            //    - Materialize config nodes only if Enabled
            //    - Bypass disabled config nodes when wiring edges
            // ------------------------------------------------------------
            var nodeMap = new Dictionary<Node, ConfigGraphNode>(onPath.Count);

            bool Materialize(Node n)
            {
                var role = GetNodeRole(n);
                if (role == NodeRole.Start || role == NodeRole.End) return true;

                if (n is IConfigNode icn && icn.Config != null)
                    return icn.Config.Enabled;

                // Non-config nodes: keep them (safe default).
                // If you want ALL non-config nodes to be pass-through, change to: return false;
                return true;
            }

            // Create runtime nodes
            foreach (var n in onPath)
            {
                if (!Materialize(n)) continue;

                var role = GetNodeRole(n);
                var cfg = (n as IConfigNode)?.Config;
                GetOrCreate(n, nodeMap, role, cfg);
            }

            // Wire runtime edges with bypassing
            foreach (var src in onPath)
            {
                if (!Materialize(src)) continue;
                if (!nodeMap.TryGetValue(src, out var srcCg)) continue;

                foreach (var dst in NextMaterializedOnPath(src, onPath, Materialize))
                {
                    if (!nodeMap.TryGetValue(dst, out var dstCg)) continue;

                    srcCg.Children.Add(dstCg);
                    dstCg.Parents.Add(srcCg);
                }
            }

            if (!nodeMap.TryGetValue(startNode, out var root))
            {
                Debug.LogError("Failed to build runtime root from Start (unexpected).");
                _cacheValid = false;
                _cachedRoot = null;
                _cachedConfigsOnPath = new List<AbstractConfig>(0);
                return null;
            }

            // Cache enabled configs on path (in graph order isn’t guaranteed; use later traversal if you need ordering)
            _cachedConfigsOnPath = new List<AbstractConfig>();
            foreach (var n in onPath)
            {
                if (n is IConfigNode icn && icn.Config != null && icn.Config.Enabled)
                    _cachedConfigsOnPath.Add(icn.Config);
            }

            // Finalize (ops, collapse logic filters, sorting)
            FinalizeGraph(root);

            _cachedRoot = root;
            _cacheValid = true;
            return _cachedRoot;
        }

        public List<AbstractConfig> GetConfigList()
        {
            if (!_cacheValid || _cachedRoot == null)
                ToConfigGraphRoot();

            return _cachedConfigsOnPath ?? new List<AbstractConfig>(0);
        }

        public int GetNodeCount()
        {
            var root = ToConfigGraphRoot();
            if (root == null) return 0;

            int count = 0;
            var visited = new HashSet<ConfigGraphNode>();
            var q = new Queue<ConfigGraphNode>();
            q.Enqueue(root);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (!visited.Add(cur)) continue;

                // Count configs only (and only enabled ones)
                if (cur.Config != null && cur.Config.Enabled)
                    count++;

                for (int i = 0; i < cur.Children.Count; i++)
                    q.Enqueue(cur.Children[i]);
            }

            return count;
        }

        // ----------------- Wiring helpers (kept minimal) -----------------

        private IEnumerable<Node> Outgoing(Node n)
        {
            foreach (var output in n.Outputs)
                foreach (var c in output.GetConnections())
                    if (c?.node != null)
                        yield return c.node;
        }

        /// <summary>
        /// Returns the "next" materialized nodes reachable from src by walking forward through
        /// non-materialized nodes (disabled configs), but stopping at the first materialized nodes.
        /// Restricted to nodes in onPath.
        /// </summary>
        private IEnumerable<Node> NextMaterializedOnPath(Node src, HashSet<Node> onPath, Func<Node, bool> materialize)
        {
            var seen = new HashSet<Node>();
            var q = new Queue<Node>();

            foreach (var o in Outgoing(src))
            {
                if (o == null) continue;
                if (!onPath.Contains(o)) continue;
                q.Enqueue(o);
            }

            while (q.Count > 0)
            {
                var n = q.Dequeue();
                if (n == null) continue;
                if (!onPath.Contains(n)) continue;
                if (!seen.Add(n)) continue;

                if (materialize(n))
                {
                    yield return n;
                    continue; // stop at first hop
                }

                // bypass disabled config node
                foreach (var o in Outgoing(n))
                {
                    if (o == null) continue;
                    if (!onPath.Contains(o)) continue;
                    q.Enqueue(o);
                }
            }
        }

        private Dictionary<Node, List<Node>> BuildReverseAdjacency(HashSet<Node> restrictTo)
        {
            var rev = new Dictionary<Node, List<Node>>(restrictTo.Count);
            foreach (var src in restrictTo)
            {
                foreach (var dst in Outgoing(src))
                {
                    if (dst == null) continue;
                    if (!restrictTo.Contains(dst)) continue;

                    if (!rev.TryGetValue(dst, out var parents))
                    {
                        parents = new List<Node>(2);
                        rev[dst] = parents;
                    }
                    parents.Add(src);
                }
            }
            return rev;
        }

        // ----------------- Your existing helpers -----------------

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

        private void FinalizeGraph(ConfigGraphNode root)
        {
            var queue = new Queue<ConfigGraphNode>();
            var visited = new HashSet<ConfigGraphNode>();

            queue.Enqueue(root);
            visited.Add(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

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

                        if (node.Parents == null) break;
                        for (int i = node.Parents.Count - 1; i >= 0; i--)
                        {
                            var parent = node.Parents[i];
                            if (parent.Role == NodeRole.Filter || parent.Role == NodeRole.LogicFilter)
                            {
                                node.Parents.Remove(parent);
                                if (parent.Parents == null) continue;
                                foreach (var grandParent in parent.Parents)
                                {
                                    grandParent.Children.Remove(parent);
                                    grandParent.Children.Add(node);
                                    node.Parents.Add(grandParent);
                                }
                            }
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
