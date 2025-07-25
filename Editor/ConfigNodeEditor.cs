// ConfigNodeEditor.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;
using XNode;
using Dalichrome.RandomGenerator.EditorHelpers;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace Dalichrome.RandomGenerator.Editor
{
    /// <summary>Reusable inspector for every <see cref="ConfigNodeBase{T}"/>.</summary>
    public class ConfigNodeEditor<TNode, TCfg> : NodeEditor
        where TNode : ConfigNodeBase<TCfg>
        where TCfg : AbstractConfig
    {

        private static readonly HashSet<string> Mask = new()
        { "_masked","_maskTime","_maskInclusion","_includeList","_excludeList" };
        private static readonly HashSet<string> Occ = new()
        { "_occupance","_occupyLayer","_tileA","_invertOccupance" };
        private static readonly HashSet<string> Skip = new()
        { "_enabled", "enabled", "Enabled" };

#if ODIN_INSPECTOR
        internal static readonly HashSet<PropertyTree> _allTrees = new();
#endif

        private readonly Dictionary<int, bool> maskFold = new(), occFold = new();
        private readonly Dictionary<int, bool> basicFold = new();

        private Type[] _cfgTypes;

        static string[] _tileNames;
        static int[] _tileIds;
        static double _nextRefresh;

        private bool _skipRegionFields;
        protected virtual bool IsAllowedType(Type type) => true;

        private void EnsureConfigTypesInitialized()
        {
            if (_cfgTypes != null) return;

            _cfgTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract &&
                            typeof(TCfg).IsAssignableFrom(t) &&
                            IsAllowedType(t))
                .OrderBy(t => t.Name)
                .ToArray();
        }

        /* ─── per-editor caches ─── */

#if ODIN_INSPECTOR
        private readonly Dictionary<TNode, PropertyTree> _treeCache = new();
#endif
        private readonly Dictionary<int, bool> _maskFold = new(), _occFold = new(), _basicFold = new();

        private static readonly HashSet<string> RegionFilterFieldNames = typeof(AbstractRegionFilterConfig)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(f => f.Name)
            .ToHashSet();

        /* ───────── HEADER ───────── */
        public override void OnHeaderGUI()
        {
            var n = (TNode)target;

            Rect bar = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, Pastel(n.PaletteSeed, n.Enabled));

            // ─── title (left) ─────────────────────────────────────
            string title = string.IsNullOrEmpty(n.DisplayName)
                          ? n.Config?.GetType().Name.Replace("Config", "") ?? "<none>"
                          : n.DisplayName;

            var lbl = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = n.Enabled ? Color.white : Color.gray }
            };
            GUI.Label(new Rect(bar.x + 6, bar.y, bar.width - 12, bar.height), title, lbl);

            // ─── icon (right) ─────────────────────────────────────
            GUIContent ico = LoadIcon(IconFilename);
            if (ico?.image != null)
            {
                const float SZ = 32f;
                Rect r = new Rect(bar.xMax - SZ - 4, bar.y + (bar.height - SZ) / 2, SZ, SZ);

                // shadow + black pad
                EditorGUI.DrawRect(new Rect(r.x, r.y, SZ, SZ), new Color(0, 0, 0, 0.25f));
                GUI.DrawTexture(r, ico.image, ScaleMode.ScaleToFit, true);
            }
        }

        /* ───────── BODY ───────── */
        public override void OnBodyGUI()
        {
            EnsureConfigTypesInitialized();

            var n = (TNode)target;
            serializedObject.Update();
            var nodeType = n.GetType().Name;
            var configType = n.Config?.GetType().Name ?? "<null>";

            _skipRegionFields = false;

            var thisNode = (TNode)target;

            foreach (var port in thisNode.Outputs)
            {
                if (!port.IsConnected) continue;

                foreach (var conn in port.GetConnections())
                {
                    var otherNode = conn.node;

                    // Safe-guard: only consider config nodes
                    if (otherNode is IConfigNode configNode && configNode.Config != null)
                    {
                        var cfg = configNode.Config;
                        bool match = cfg is AbstractRegionFilterConfig;

                        if (match)
                        {
                            _skipRegionFields = true;
                            break;
                        }
                    }
                }

                if (_skipRegionFields) break;
            }

            /* 0  Ports */
            DrawPorts(n);
            EditorGUILayout.Space(6);

            /* 1  Basic fold-out (Enabled + Name) */
            int id = n.GetInstanceID();
            bool open = _basicFold.TryGetValue(id, out var v) && v;
            open = EditorGUILayout.Foldout(open, "Basic", true);
            _basicFold[id] = open;
            if (open)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_enabled"), new GUIContent("Enabled"));

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField("Name",
                                   serializedObject.FindProperty("_displayName").stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.FindProperty("_displayName").stringValue = newName;
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_priority"), new GUIContent("Priority"));

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(4);

            /* 2  Config picker */
            var cfgProp = serializedObject.FindProperty("_config");
            Type cur = cfgProp.managedReferenceValue?.GetType();

            if (cfgProp.managedReferenceValue == null && _cfgTypes.Length > 0)
            {
                cfgProp.managedReferenceValue = Activator.CreateInstance(_cfgTypes[0]);
                n.SyncNameWithType();               // keep the node title in sync
            }

            /* A: build the option list ------------------------------------------------- */
            string[] opt = _cfgTypes.Select(t => t.Name.Replace("Config", "")).ToArray();

            /* B: if we have **zero** concrete types just show a message and bail out ---- */
            if (opt.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No concrete {typeof(TCfg).Name} types were found in the project.\n" +
                    "Make sure you have at least one non-abstract class that derives from it.",
                    MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            /* C: otherwise we’re safe --------------------------------------------------- */
            int idx = Array.IndexOf(_cfgTypes, cur);
            if (idx < 0) idx = 0;                       // clamp to a valid element
            int sel = EditorGUILayout.Popup("Config Type", idx, opt);

            if (sel != idx)
            {
                cfgProp.managedReferenceValue = Activator.CreateInstance(_cfgTypes[sel]);
                n.SyncNameWithType();
            }
            EditorGUILayout.Space(6);

            /* 3  Draw the config’s fields (same buckets you already had) */
            if (cfgProp.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Pick a config to edit its settings.", MessageType.Info);
                serializedObject.ApplyModifiedProperties(); return;
            }

#if ODIN_INSPECTOR
            DrawWithOdin(n, cfgProp);
#else
            DrawWithoutOdin(n, cfgProp);
#endif
            serializedObject.ApplyModifiedProperties();
        }

        // ………………………  (all your BucketAndDraw / RenderProperty helpers copied verbatim)
        //                Nothing in those methods depends on TCfg or the node type,
        //                so you can literally paste your previous code block here.
        // ………………………

        /* ─── pretty pastel ─── */
        private static Color Pastel(int seed, bool enabled)
        {
            if (!enabled) return Color.black;

            var rng = new System.Random(seed);
            float h = (float)rng.NextDouble();
            float s = 0.55f + 0.25f * (float)rng.NextDouble();
            float v = 0.55f + 0.10f * (float)rng.NextDouble();
            return Color.HSVToRGB(h, s, v);
        }

        public override int GetWidth() => 348;

#if ODIN_INSPECTOR
        private void DrawWithOdin(TNode node, SerializedProperty cfgProp)
        {
            var cfgObj = (TCfg)cfgProp.managedReferenceValue;
            if (cfgObj == null) return;

            // Dispose any tree that belongs to *another* config object
            if (_treeCache.TryGetValue(node, out var cached) &&
                !ReferenceEquals(cached.WeakTargets[0], cfgObj))
            {
                cached.Dispose();
                _treeCache.Remove(node);
            }

            // Create (and track) the fresh tree if we don't already have it
            if (!_treeCache.TryGetValue(node, out var tree))
            {
                tree = PropertyTree.Create(cfgObj);
                _treeCache[node] = tree;
                NodeEditorReloadHook.LiveTrees.Add(tree);
            }

            try
            {
                tree.BeginDraw(false);

                // ─── Bucket fields ─────────────────────────────────────
                var maskingProps = new List<InspectorProperty>();
                var occupanceProps = new List<InspectorProperty>();
                var otherProps = new List<InspectorProperty>();

                foreach (var p in tree.RootProperty.Children)
                {
                    if (Skip.Contains(p.Name)) continue;

                    if (_skipRegionFields && IsRegionFilterField(p)) continue;

                    if (Mask.Contains(p.Name)) maskingProps.Add(p);
                    else if (Occ.Contains(p.Name)) occupanceProps.Add(p);
                    else otherProps.Add(p);
                }

                // ─── Draw groups ───────────────────────────────────────
                foreach (var p in otherProps) p.Draw();

                int id = node.GetInstanceID();

                if (maskingProps.Count > 0)
                {
                    bool open = _maskFold.TryGetValue(id, out var v) && v;
                    open = SirenixEditorGUI.Foldout(open, "Masking");
                    _maskFold[id] = open;

                    if (open)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var p in maskingProps) p.Draw();
                        EditorGUI.indentLevel--;
                    }
                }

                if (cfgObj is IOccupanceConfig && occupanceProps.Count > 0)
                {
                    bool open = _occFold.TryGetValue(id, out var v) && v;
                    open = SirenixEditorGUI.Foldout(open, "Occupance");
                    _occFold[id] = open;

                    if (open)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var p in occupanceProps) p.Draw();
                        EditorGUI.indentLevel--;
                    }
                }
            }
            finally
            {
                tree.EndDraw();
            }
        }
#endif

        /*───────────────────────────────────────────────────────────────
        *  ❷ DrawWithoutOdin — vanilla IMGUI path
        *──────────────────────────────────────────────────────────────*/
        private void DrawWithoutOdin(TNode node, SerializedProperty cfgProp)
        {
            // Re-use the exact helper you already had.
            BucketAndDraw(cfgProp, node);
        }

        /* ──────────────────────────────────────────────────────────
         *  BUCKET + DRAW  — handles Generation / Masking / Occupance,
         *  and shows a custom dropdown for every int with [TileDisplay]
         *  even inside XNode’s custom inspector.
         * ────────────────────────────────────────────────────────── */
        private void BucketAndDraw(SerializedProperty cfgProp, TNode node)
        {
            // ── 1) bucket the immediate child fields ──────────────────
            List<SerializedProperty> generation = new();
            List<SerializedProperty> masking = new();
            List<SerializedProperty> occupance = new();

            SerializedProperty child = cfgProp.Copy();
            bool enter = true;

            while (child.NextVisible(enter))
            {
                enter = false;
                if (child.depth != cfgProp.depth + 1) continue; 
                if (Skip.Contains(child.name)) continue;
                if (_skipRegionFields && RegionFilterFieldNames.Contains(child.name)) continue;

                SerializedProperty snapshot = child.Copy();           // keep stable

                if (Mask.Contains(snapshot.name)) masking.Add(snapshot);
                else if (Occ.Contains(snapshot.name)) occupance.Add(snapshot);
                else generation.Add(snapshot);
            }

            // ── 2) draw each bucket ──────────────────────────────────
            DrawBucket("Generation", generation);
            DrawFoldBucket("Masking", masking, maskFold, node.GetInstanceID());
            if (occupance.Count > 0)
                DrawFoldBucket("Occupance", occupance, occFold, node.GetInstanceID());
        }

        /* -------  helpers  ------- */

        private static void DrawBucket(string label, List<SerializedProperty> list)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var p in list) RenderProperty(p);
            EditorGUI.indentLevel--;
        }

        private static void DrawFoldBucket(string label,
                                           List<SerializedProperty> list,
                                           Dictionary<int, bool> dict,
                                           int nodeId)
        {
            bool open = dict.TryGetValue(nodeId, out bool v) ? v : false;
            open = EditorGUILayout.Foldout(open, label, true);
            dict[nodeId] = open;
            if (!open) return;

            EditorGUI.indentLevel++;
            foreach (var p in list) RenderProperty(p);
            EditorGUI.indentLevel--;
        }

        private static void DrawGroup(string label, IEnumerable<SerializedProperty> props)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++; foreach (var p in props) EditorGUILayout.PropertyField(p, true);
            EditorGUI.indentLevel--;
        }
        private static void DrawFold(string label, List<SerializedProperty> props,
                                     Dictionary<int, bool> dict, int id)
        {
            bool open = dict.TryGetValue(id, out bool v) ? v : false;
            open = EditorGUILayout.Foldout(open, label, true); dict[id] = open;
            if (!open) return;
            EditorGUI.indentLevel++; foreach (var p in props) EditorGUILayout.PropertyField(p, true);
            EditorGUI.indentLevel--;
        }

        static void EnsureTileCache()
        {
            if (_tileNames != null && EditorApplication.timeSinceStartup < _nextRefresh)
                return;

            var list = new List<(string, int)>();

            // 1) enum values
            foreach (TileType e in Enum.GetValues(typeof(TileType)))
                list.Add(($"enum/{e}", (int)e));

            // 2) Resources/ TileObjects
            foreach (var obj in Resources.LoadAll<TileObject>(""))
                list.Add(($"asset/{obj.name}", obj.tileId));

            list.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase));

            _tileNames = list.ConvertAll(t => t.Item1).ToArray();
            _tileIds = list.ConvertAll(t => t.Item2).ToArray();
            _nextRefresh = EditorApplication.timeSinceStartup + 3.0;     // refresh every 3 s
        }

        static bool FieldHasTileDisplay(SerializedProperty prop)
        {
            object obj = prop.serializedObject.targetObject;
            Type t = obj.GetType();

            foreach (string part in prop.propertyPath.Split('.'))
            {
                if (part == "Array" || part.StartsWith("data[")) continue;

                var fi = t.GetField(part,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi == null) return false;

                // ❶ attribute directly on the FIELD?
                if (Attribute.IsDefined(fi, typeof(TileDisplayAttribute)))
                    return true;

                // ❷ attribute on a matching PROPERTY?   "_includeList" → "IncludeList"
                string cand = part.TrimStart('_');
                if (cand.Length > 0) cand = char.ToUpper(cand[0]) + cand[1..];

                var pi = t.GetProperty(cand,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null && Attribute.IsDefined(pi, typeof(TileDisplayAttribute)))
                    return true;

                // go deeper if this is a managed-reference field
                obj = fi.GetValue(obj);
                if (obj == null) return false;
                t = obj.GetType();
            }
            return false;
        }

        private static void RenderProperty(SerializedProperty prop)
        {
#if ODIN_INSPECTOR
            // If Odin is present, let its own drawers handle everything;
            // we only need the custom path for non-Odin projects.
            EditorGUILayout.PropertyField(prop, true);
#else

            bool isIntField = prop.propertyType == SerializedPropertyType.Integer;

            // list OR array whose element type is int
            bool isIntArray = prop.isArray && prop.arrayElementType == "int";   // ← simplified

            if ((isIntField || isIntArray) && FieldHasTileDisplay(prop))
            {
                EnsureTileCache();

                // ---------- single int ----------
                if (isIntField)
                {
                    DrawTileDropdown(prop, new GUIContent(prop.displayName));
                }
                // ---------- int[] or List<int> ----------
                else
                {
                    EditorGUILayout.LabelField(prop.displayName, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    // show +/- size control like Unity's default array drawer
                    int newSize = EditorGUILayout.IntField("Size", prop.arraySize);
                    if (newSize != prop.arraySize) prop.arraySize = Math.Max(0, newSize);

                    // element dropdowns
                    for (int i = 0; i < prop.arraySize; i++)
                    {
                        var element = prop.GetArrayElementAtIndex(i);
                        DrawTileDropdown(element, new GUIContent($"Element {i}"));
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                // fallback to whatever drawer Unity/XNode would normally use
                EditorGUILayout.PropertyField(prop, true);
            }
#endif
        }

        /* --- small helper so we don't duplicate popup code --- */
        private static void DrawTileDropdown(SerializedProperty intProp, GUIContent label)
        {
            int idx = Array.IndexOf(_tileIds, intProp.intValue);
            if (idx < 0) idx = 0;

            int newVal = EditorGUILayout.IntPopup(
                label.text,
                intProp.intValue,
                _tileNames,
                _tileIds);

            intProp.intValue = newVal;
        }

        /* -------------------------------------------------------------
         *  DrawPorts  – default: draw every static port (any direction)
         *               Derived editors can override for fancy layout.
         * ------------------------------------------------------------ */
        protected virtual void DrawPorts(TNode n)
        {
            // draw input(s) first
            foreach (var p in n.Ports.Where(p => p.direction == NodePort.IO.Input && !p.IsDynamic))
                NodeEditorGUILayout.PortField(p);

            // draw static outputs next
            foreach (var p in n.Ports.Where(p => p.direction == NodePort.IO.Output && !p.IsDynamic))
                NodeEditorGUILayout.PortField(p);

            // finally dynamic ones (lists) – draw each port individually
            foreach (var p in n.DynamicPorts.Where(p => p.direction == NodePort.IO.Output))
                NodeEditorGUILayout.PortField(p);

            EditorGUILayout.Space(6);
        }

        // one-liner hook; default → null  (= no icon)
        protected string IconFilename
        {
            get
            {
                var cfg = ((TNode)target).Config;
                return cfg?.IconName ?? null;
            }
        }

        // helper reused by the header drawer
        private GUIContent LoadIcon(string fileName) =>
            string.IsNullOrEmpty(fileName) ? null :
                IconUtils.Get(fileName);

#if ODIN_INSPECTOR
        void OnDisable()            // message, not an override
        {
            foreach (var tree in _treeCache.Values)
            {
                tree?.Dispose();
                NodeEditorReloadHook.LiveTrees.Remove(tree);
            }
            _treeCache.Clear();
        }

#endif
        private static bool IsRegionFilterField(InspectorProperty property)
        {
            var member = property.Info.GetMemberInfo();

            if (member == null) return false;

            return member.DeclaringType == typeof(AbstractRegionFilterConfig);
        }
    }
}
#endif
