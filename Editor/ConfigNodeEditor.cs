// Editor/ConfigNodeEditor.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Dalichrome.RandomGenerator.EditorHelpers; // NodeEditorReloadHook.LiveTrees
#endif

namespace Dalichrome.RandomGenerator.Editor
{
    /// <summary>Compact inspector for any <see cref="ConfigNodeBase{T}"/>.</summary>
    public class ConfigNodeEditor<TNode, TCfg> : NodeEditor
        where TNode : ConfigNodeBase<TCfg>
        where TCfg : AbstractConfig
    {
#if ODIN_INSPECTOR
        // Any field/property marked with one of these attributes will be drawn via Unity PropertyField
        private static readonly HashSet<Type> UnityDrawerAttributeTypes = new()
        {
            typeof(Dalichrome.RandomGenerator.Configs.UUIDFieldAttribute),
            typeof(Dalichrome.RandomGenerator.Configs.StructureDisplayAttribute),
            // add more here as you create them...
        };

        private static bool HasAnyAttribute(MemberInfo mi, HashSet<Type> candidates)
        {
            if (mi == null) return false;
            foreach (var t in candidates)
                if (Attribute.IsDefined(mi, t, inherit: true))
                    return true;
            return false;
        }
#endif

        /* --------- field buckets by name --------- */
        private static readonly HashSet<string> SkipNames = new()
        { "_enabled", "enabled", "Enabled", "_displayName", "_priority" };

        private static readonly HashSet<string> MaskNames = new()
        { "_masked", "_maskTime", "_maskInclusion", "_includeList", "_excludeList" };

        private static readonly HashSet<string> OccupanceNames = new()
        { "_occupance", "_occupyLayer", "_tileA", "_invertOccupance" };

        private static readonly HashSet<string> RegionFilterFieldNames =
            typeof(AbstractRegionFilterConfig)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Select(f => f.Name).ToHashSet();

        /* --------- small per-node UI state --------- */
        private readonly Dictionary<int, bool> _foldBasic = new();
        private readonly Dictionary<int, bool> _foldMask = new();
        private readonly Dictionary<int, bool> _foldOcc = new();

        private bool _skipRegionFields;
        private Type[] _cfgTypes;

#if ODIN_INSPECTOR
        private readonly Dictionary<TNode, PropertyTree> _odinTrees = new();
#endif

        /* --------- type discovery --------- */
        protected virtual bool IsAllowedType(Type t) => true;

        private void EnsureConfigTypes()
        {
            if (_cfgTypes != null) return;

            _cfgTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => !t.IsAbstract && typeof(TCfg).IsAssignableFrom(t) && IsAllowedType(t))
                .OrderBy(t => t.Name)
                .ToArray();
        }

        /* ======================= HEADER ======================= */
        public override void OnHeaderGUI()
        {
            var n = (TNode)target;
            Rect bar = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, Pastel(n.PaletteSeed, n.Enabled));

            string title = string.IsNullOrEmpty(n.DisplayName)
                ? n.Config?.GetType().Name.Replace("Config", "") ?? "<none>"
                : n.DisplayName;

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = n.Enabled ? Color.white : Color.gray }
            };
            GUI.Label(new Rect(bar.x + 6, bar.y, bar.width - 12, bar.height), title, style);

            var icon = LoadIcon(IconFilename);
            if (icon?.image != null)
            {
                const float SZ = 32f;
                Rect r = new(bar.xMax - SZ - 4, bar.y + (bar.height - SZ) / 2, SZ, SZ);
                EditorGUI.DrawRect(new Rect(r.x, r.y, SZ, SZ), new Color(0, 0, 0, 0.25f));
                GUI.DrawTexture(r, icon.image, ScaleMode.ScaleToFit, true);
            }
        }

        /* ======================== BODY ======================== */
        public override void OnBodyGUI()
        {
            EnsureConfigTypes();

            var n = (TNode)target;
            serializedObject.Update();

            // region filter wiring → hide region-only fields
            _skipRegionFields = HasConnectedRegionFilter(n);

            DrawPorts(n);
            EditorGUILayout.Space(6);

            DrawBasicFold(n);
            EditorGUILayout.Space(4);

            // config picker
            var cfgProp = serializedObject.FindProperty("_config");
            if (cfgProp.managedReferenceValue == null && _cfgTypes.Length > 0)
            {
                cfgProp.managedReferenceValue = Activator.CreateInstance(_cfgTypes[0]);
                n.SyncNameWithType();
            }

            if (_cfgTypes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"No concrete {typeof(TCfg).Name} types found.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var curType = cfgProp.managedReferenceValue?.GetType();
            int idx = Array.IndexOf(_cfgTypes, curType);
            if (idx < 0) idx = 0;
            var labels = _cfgTypes.Select(t => t.Name.Replace("Config", "")).ToArray();
            int pick = EditorGUILayout.Popup("Config Type", idx, labels);
            if (pick != idx)
            {
                cfgProp.managedReferenceValue = Activator.CreateInstance(_cfgTypes[pick]);
                n.SyncNameWithType();
            }

            if (cfgProp.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Pick a config to edit its settings.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

#if ODIN_INSPECTOR
            DrawConfigWithOdin(n, cfgProp);
#else
            DrawConfigWithoutOdin(n, cfgProp);
#endif

            serializedObject.ApplyModifiedProperties();
        }

        /* =================== DRAW HELPERS ===================== */

        private void DrawBasicFold(TNode n)
        {
            int id = n.GetInstanceID();
            bool open = _foldBasic.TryGetValue(id, out var v) && v;
            open = EditorGUILayout.Foldout(open, "Basic", true);
            _foldBasic[id] = open;

            if (!open) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_enabled"), new GUIContent("Enabled"));

            var nameProp = serializedObject.FindProperty("_displayName");
            string newName = EditorGUILayout.TextField("Name", nameProp.stringValue);
            if (newName != nameProp.stringValue) nameProp.stringValue = newName;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_priority"), new GUIContent("Priority"));
            EditorGUI.indentLevel--;
        }

        private bool HasConnectedRegionFilter(TNode node)
        {
            foreach (var port in node.Outputs)
            {
                if (!port.IsConnected) continue;
                foreach (var c in port.GetConnections())
                {
                    if (c?.node is IConfigNode cn && cn.Config is AbstractRegionFilterConfig)
                        return true;
                }
            }
            return false;
        }

#if ODIN_INSPECTOR
        private void DrawConfigWithOdin(TNode node, SerializedProperty cfgProp)
        {
            var cfgObj = (TCfg)cfgProp.managedReferenceValue;
            if (cfgObj == null) return;

            if (_odinTrees.TryGetValue(node, out var cached) &&
                !ReferenceEquals(cached.WeakTargets[0], cfgObj))
            {
                cached.Dispose();
                _odinTrees.Remove(node);
            }

            if (!_odinTrees.TryGetValue(node, out var tree))
            {
                tree = PropertyTree.Create(cfgObj);
                _odinTrees[node] = tree;
                NodeEditorReloadHook.LiveTrees.Add(tree);
            }

            // Build a lookup of immediate child SerializedProperties (depth + 1) for Unity drawing.
            var unityChildren = new Dictionary<string, SerializedProperty>(32);
            {
                var it = cfgProp.Copy();
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (it.depth != cfgProp.depth + 1) continue;
                    unityChildren[it.name] = it.Copy();
                }
            }

            try
            {
                tree.BeginDraw(false);

                var gen = new List<InspectorProperty>();
                var mask = new List<InspectorProperty>();
                var occ = new List<InspectorProperty>();

                foreach (var p in tree.RootProperty.Children)
                {
                    if (SkipNames.Contains(p.Name)) continue;
                    if (_skipRegionFields && IsRegionDecl(p)) continue;

                    if (MaskNames.Contains(p.Name)) mask.Add(p);
                    else if (OccupanceNames.Contains(p.Name)) occ.Add(p);
                    else gen.Add(p);
                }

                // Generation group
                foreach (var p in gen)
                {
                    var mi = p.Info.GetMemberInfo(); // FieldInfo or PropertyInfo
                    bool forceUnity = HasAnyAttribute(mi, UnityDrawerAttributeTypes);

                    if (forceUnity && unityChildren.TryGetValue(p.Name, out var sp))
                        EditorGUILayout.PropertyField(sp, includeChildren: true);
                    else
                        p.Draw();
                }

                int id = node.GetInstanceID();

                if (mask.Count > 0)
                {
                    bool open = _foldMask.TryGetValue(id, out var v) && v;
                    open = SirenixEditorGUI.Foldout(open, "Masking");
                    _foldMask[id] = open;
                    if (open)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var p in mask)
                        {
                            var mi = p.Info.GetMemberInfo();
                            bool forceUnity = HasAnyAttribute(mi, UnityDrawerAttributeTypes);

                            if (forceUnity && unityChildren.TryGetValue(p.Name, out var sp))
                                EditorGUILayout.PropertyField(sp, true);
                            else
                                p.Draw();
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                if (cfgObj is IOccupanceConfig && occ.Count > 0)
                {
                    bool open = _foldOcc.TryGetValue(id, out var v2) && v2;
                    open = SirenixEditorGUI.Foldout(open, "Occupance");
                    _foldOcc[id] = open;
                    if (open)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var p in occ)
                        {
                            var mi = p.Info.GetMemberInfo();
                            bool forceUnity = HasAnyAttribute(mi, UnityDrawerAttributeTypes);

                            if (forceUnity && unityChildren.TryGetValue(p.Name, out var sp))
                                EditorGUILayout.PropertyField(sp, true);
                            else
                                p.Draw();
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            finally { tree.EndDraw(); }
        }

        private static bool IsRegionDecl(InspectorProperty prop)
        {
            var mi = prop.Info.GetMemberInfo();
            return mi != null && mi.DeclaringType == typeof(AbstractRegionFilterConfig);
        }

        void OnDisable()
        {
            foreach (var t in _odinTrees.Values)
            {
                t?.Dispose();
                NodeEditorReloadHook.LiveTrees.Remove(t);
            }
            _odinTrees.Clear();
        }
#endif

        private void DrawConfigWithoutOdin(TNode node, SerializedProperty cfgProp)
        {
            var gen = new List<SerializedProperty>();
            var mask = new List<SerializedProperty>();
            var occ = new List<SerializedProperty>();

            var it = cfgProp.Copy();
            bool enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (it.depth != cfgProp.depth + 1) continue;

                if (SkipNames.Contains(it.name)) continue;
                if (_skipRegionFields && RegionFilterFieldNames.Contains(it.name)) continue;

                var p = it.Copy();

                if (MaskNames.Contains(p.name)) mask.Add(p);
                else if (OccupanceNames.Contains(p.name)) occ.Add(p);
                else gen.Add(p);
            }

            DrawGroup("Generation", gen);
            DrawFoldGroup("Masking", mask, _foldMask, node.GetInstanceID());
            if (occ.Count > 0) DrawFoldGroup("Occupance", occ, _foldOcc, node.GetInstanceID());
        }

        private static void DrawGroup(string label, IEnumerable<SerializedProperty> props)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var p in props) EditorGUILayout.PropertyField(p, true);
            EditorGUI.indentLevel--;
        }

        private static void DrawFoldGroup(string label, IEnumerable<SerializedProperty> props,
                                          Dictionary<int, bool> state, int nodeId)
        {
            bool open = state.TryGetValue(nodeId, out var v) && v;
            open = EditorGUILayout.Foldout(open, label, true);
            state[nodeId] = open;
            if (!open) return;

            EditorGUI.indentLevel++;
            foreach (var p in props) EditorGUILayout.PropertyField(p, true);
            EditorGUI.indentLevel--;
        }

        /* ================= PORTS / ICON / WIDTH ================= */

        protected virtual void DrawPorts(TNode n)
        {
            foreach (var p in n.Ports.Where(p => p.direction == NodePort.IO.Input && !p.IsDynamic))
                NodeEditorGUILayout.PortField(p);

            foreach (var p in n.Ports.Where(p => p.direction == NodePort.IO.Output && !p.IsDynamic))
                NodeEditorGUILayout.PortField(p);

            foreach (var p in n.DynamicPorts.Where(p => p.direction == NodePort.IO.Output))
                NodeEditorGUILayout.PortField(p);

            EditorGUILayout.Space(6);
        }

        protected string IconFilename => ((TNode)target).Config?.IconName;

        private static GUIContent LoadIcon(string fileName)
            => string.IsNullOrEmpty(fileName) ? null : IconUtils.Get(fileName);

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
    }
}
#endif
