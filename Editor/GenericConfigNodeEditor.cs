#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator;
using Dalichrome.RandomGenerator.UserData;
using System.Reflection;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

[CustomNodeEditor(typeof(GeneratorConfigNode))]
public class GeneratorConfigNodeEditor : NodeEditor
{
    /* ─── cached type list ─── */
    private static readonly Type[] cfgTypes = AppDomain.CurrentDomain
        .GetAssemblies().SelectMany(a => a.GetTypes())
        .Where(t => !t.IsAbstract && typeof(AbstractGeneratorConfig).IsAssignableFrom(t))
        .OrderBy(t => t.Name).ToArray();

    private static readonly HashSet<string> Mask = new()
        { "_masked","_maskTime","_maskInclusion","_includeList","_excludeList" };
    private static readonly HashSet<string> Occ = new()
        { "_occupance","_occupyLayer","_tileA","_invertOccupance" };
    private static readonly HashSet<string> Skip = new()
    { "_enabled", "enabled", "Enabled" };

#if ODIN_INSPECTOR
    private readonly Dictionary<GeneratorConfigNode, PropertyTree> treeCache = new();
#endif

    private readonly Dictionary<int, bool> maskFold = new(), occFold = new();
    private readonly Dictionary<int, bool> basicFold = new();

    /* ───────── HEADER (read-only) ───────── */
    public override void OnHeaderGUI()
    {
        var node = (GeneratorConfigNode)target;
        Rect bar = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        Color tint = node.Enabled ? Pastel(node.Type) : Color.black;      // NEW
        EditorGUI.DrawRect(bar, tint);

        string title = !string.IsNullOrEmpty(node.DisplayName)
               ? node.DisplayName                    // ← always prefer user text
               : node.Type.ToString().Replace('_', ' ');

        GUIStyle lbl = new GUIStyle(EditorStyles.boldLabel);
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.normal.textColor = node.Enabled ? Color.white : Color.gray;

        Rect r = new(bar.x + 6, bar.y, bar.width - 12, bar.height);
        GUI.Label(r, title, lbl);
    }

    /* ───────── BODY (interactive) ───────── */
    public override void OnBodyGUI()
    {
        var node = (GeneratorConfigNode)target;
        serializedObject.Update();

        /* 0  Ports */
        NodeEditorGUILayout.PortField(target.GetInputPort("input"));
        NodeEditorGUILayout.PortField(target.GetOutputPort("output"));
        EditorGUILayout.Space(6);

        /* 1 Basic fold-out */
        int id = node.GetInstanceID();
        bool open = basicFold.TryGetValue(id, out bool v) ? v : false;      // default OPEN
        open = EditorGUILayout.Foldout(open, "Basic", true);
        basicFold[id] = open;

        if (open)
        {
            EditorGUI.indentLevel++;
            SerializedProperty enabledProp = serializedObject.FindProperty("_enabled");
            SerializedProperty nameProp = serializedObject.FindProperty("_displayName");

            EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled"));

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Name", nameProp.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                nameProp.stringValue = newName;
                node.DisplayName = newName;                         // keeps flag in sync
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(4);

        /* 2  Generator-type popup */
        SerializedProperty cfgProp = serializedObject.FindProperty("_config");
        Type cur = cfgProp.managedReferenceValue?.GetType();
        string[] opt = cfgTypes.Select(t => t.Name.Replace("Config", "")).ToArray();
        int idx = Array.IndexOf(cfgTypes, cur);
        int sel = EditorGUILayout.Popup("Generator", idx < 0 ? 0 : idx, opt);

        if (sel != idx)
        {
            cfgProp.managedReferenceValue = Activator.CreateInstance(cfgTypes[sel]);
            node.SyncNameWithType();            // keep display name in sync
        }

        EditorGUILayout.Space(6);

#if ODIN_INSPECTOR
        /* 3  Draw config with custom grouping / hiding */
        if (cfgProp.managedReferenceValue == null)
        {
            SirenixEditorGUI.InfoMessageBox("Pick a generator to edit its settings.");
        }
        else
        {
            var cfgObj = (AbstractGeneratorConfig)cfgProp.managedReferenceValue;

            // ── build / reuse PropertyTree ───────────────────────────────
            if (!treeCache.TryGetValue(node, out var tree) || tree.WeakTargets[0] != cfgObj)
            {
                tree = PropertyTree.Create(cfgObj);
                treeCache[node] = tree;
            }

            tree.BeginDraw(false);   // FALSE → no prefab buttons

            // ── bucket properties by name ───────────────────────────────
            var maskingProps = new List<InspectorProperty>();
            var occupanceProps = new List<InspectorProperty>();
            var otherProps = new List<InspectorProperty>();

            foreach (var p in tree.RootProperty.Children)
            {
                if (Skip.Contains(p.Name)) continue;           // hide
                if (Mask.Contains(p.Name)) maskingProps.Add(p);
                else if (Occ.Contains(p.Name)) occupanceProps.Add(p);
                else otherProps.Add(p);
            }

            // ── main group ──────────────────────────────────────────────
            foreach (var p in otherProps) p.Draw();

            // ── Masking fold-out ────────────────────────────────────────
            int nId = node.GetInstanceID();
            bool mOpen = maskFold.TryGetValue(nId, out var v1) ? v1 : true;

            if (maskingProps.Count > 0)
            {
                mOpen = SirenixEditorGUI.Foldout(mOpen, "Masking");
                maskFold[nId] = mOpen;

                if (mOpen)
                {
                    EditorGUI.indentLevel++;
                    foreach (var p in maskingProps) p.Draw();
                    EditorGUI.indentLevel--;
                }
            }

            // ── Occupance fold-out (only for IOccupanceConfig) ──────────
            if (cfgObj is IOccupanceConfig && occupanceProps.Count > 0)
            {
                bool oOpen = occFold.TryGetValue(nId, out var v2) ? v2 : true;

                oOpen = SirenixEditorGUI.Foldout(oOpen, "Occupance");
                occFold[nId] = oOpen;

                if (oOpen)
                {
                    EditorGUI.indentLevel++;
                    foreach (var p in occupanceProps) p.Draw();
                    EditorGUI.indentLevel--;
                }
            }

            tree.EndDraw();
        }
#else
        /* 3  Draw config fields in buckets */
        if (cfgProp.managedReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Pick a generator to edit its settings.", MessageType.Info);
            serializedObject.ApplyModifiedProperties(); return;
        }

        BucketAndDraw(cfgProp, node);
#endif

        serializedObject.ApplyModifiedProperties();
    }

    public override int GetWidth() => 348;

    /* ─── helpers ─── */

    // darker-pastel: still pleasant, but now ~50 % darker
    private static Color Pastel(GeneratorType t)
    {
        var rng = new System.Random((int)t);

        float h = (float)rng.NextDouble();                   // 0-1 hue
        float s = 0.55f + 0.25f * (float)rng.NextDouble();   // 0.55-0.80 saturation
        float v = 0.55f + 0.10f * (float)rng.NextDouble();   // 0.55-0.65 value (darker)

        return Color.HSVToRGB(h, s, v);
    }

/* ──────────────────────────────────────────────────────────
 *  BUCKET + DRAW  — handles Generation / Masking / Occupance,
 *  and shows a custom dropdown for every int with [TileDisplay]
 *  even inside XNode’s custom inspector.
 * ────────────────────────────────────────────────────────── */
private void BucketAndDraw(SerializedProperty cfgProp, GeneratorConfigNode node)
{
    // ── 1) bucket the immediate child fields ──────────────────
    List<SerializedProperty> generation = new();
    List<SerializedProperty> masking    = new();
    List<SerializedProperty> occupance  = new();

    SerializedProperty child = cfgProp.Copy();
    bool enter = true;

    while (child.NextVisible(enter))
    {
        enter = false;
        if (child.depth != cfgProp.depth + 1) continue;       // only direct kids
        if (Skip.Contains(child.name))          continue;     // hide config-level _enabled

        SerializedProperty snapshot = child.Copy();           // keep stable

        if      (Mask.Contains(snapshot.name)) masking  .Add(snapshot);
        else if (Occ .Contains(snapshot.name)) occupance.Add(snapshot);
        else                                   generation.Add(snapshot);
    }

    // ── 2) draw each bucket ──────────────────────────────────
    DrawBucket("Generation", generation);
    DrawFoldBucket("Masking",    masking,   maskFold, node.GetInstanceID());
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
                                       Dictionary<int,bool> dict,
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

    // ---------- cache & helpers ----------
    static string[] _tileNames;
    static int[] _tileIds;
    static double _nextRefresh;           // in seconds

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
            return;
#endif

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
}
#endif
