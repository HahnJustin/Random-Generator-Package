// ==============================
// File: StructureEditorWindow.cs
// ================================
// Editor window: registry-driven, uses TileObject sprites, shows layer names,
// ignores layerId==0, tileObject id==0, and TileObjects with tileKind==Empty.
// Auto-switches to a tile's configured layer when that tile is selected.
// Tiles palette is a grid with search + tooltips. Double-click Structure opens this.
// Grid is clipped so it won't overdraw the sidebar. Auto-rebinds registries on open.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Dalichrome.RandomGenerator.UserData;

public class StructureEditorWindow : EditorWindow
{
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 8f;
    private const float GridCellBase = 32f;
    private const float LeftPaneWidth = 440f; // narrower to avoid horizontal scroll

    [SerializeField] private Structure asset;

    // View
    private Vector2 pan; private float zoom = 1f;
    private float otherLayersOpacity = 0.85f; // user-controlled transparency for non-active layers (0.1..1)

    // Registry snapshot (filtered)
    private int[] currentLayerIds = Array.Empty<int>();
    private string[] currentLayerNames = Array.Empty<string>();
    private int[] currentPaletteIds = Array.Empty<int>();

    // State
    private int activeLayerIndex = 0;
    private ToolMode tool = ToolMode.Brush;
    private int paintTileId = -1;
    private bool isDraggingPaint; private Vector2 lastMousePos;

    private enum ToolMode { Brush, Erase, Fill, Picker }

    // Palette & drawing caches
    private struct PaletteItem { public int id; public string name; public Sprite sprite; public Color color; public int layerId; public string layerName; }
    private PaletteItem[] palette = Array.Empty<PaletteItem>();
    private Vector2 paletteScroll;
    private Vector2 leftScroll;
    private string paletteSearch = "";
    private bool filterByActiveLayer = false;
    private bool maintenanceFoldout = false; // collapsed by default

    // Palette diagnostics
    private readonly List<string> ignoredTileReasons = new();

    // Grid palette layout
    private const int PaletteCellSize = 32;
    private const int PaletteCellPadding = 2;

    // Fast lookups for grid drawing
    private readonly Dictionary<int, Sprite> spriteByTileId = new();
    private readonly Dictionary<int, Color> colorByTileId = new();
    private readonly Dictionary<int, int> tileIdToLayerId = new();
    private readonly Dictionary<int, string> tileIdToName = new();

    [MenuItem("Random Generator/Structure Painter")]
    public static void Open()
    {
        var w = GetWindow<StructureEditorWindow>("Structure Painter");
        EnsureGoodInitialSize(w);
        w.Show();
    }
    public static void Open(Structure a)
    {
        var w = GetWindow<StructureEditorWindow>("Structure Painter");
        EnsureGoodInitialSize(w);
        w.asset = a;
        w.Focus();
    }

    private static void EnsureGoodInitialSize(EditorWindow w)
    {
        var r = w.position;
        if (r.width < 1100 || r.height < 700) w.position = new Rect(r.x, r.y, 1200, 800);
        w.minSize = new Vector2(900, 600);
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID) as Structure;
        if (obj != null) { Open(obj); return true; }
        return false;
    }

    private void OnEnable() { wantsMouseMove = true; RefreshRegistry(); }
    private void OnFocus() { RefreshRegistry(); Repaint(); }

    private void RefreshRegistry()
    {
        // LAYERS: from TileLayerInfo, filter out 0
        var ids = Dalichrome.RandomGenerator.TileLayerInfo.AllLayerIds?.Where(l => l != 0).ToArray() ?? Array.Empty<int>();
        currentLayerIds = ids;
        currentLayerNames = currentLayerIds.Select(lid => Dalichrome.RandomGenerator.TileLayerInfo.GetName(lid) ?? $"Layer {lid}").ToArray();

        // PALETTE: from TileObjects via GenericIdDropdownCache (ignore id 0 and tileKind Empty)
        var list = new List<PaletteItem>();
        spriteByTileId.Clear(); colorByTileId.Clear(); tileIdToLayerId.Clear(); tileIdToName.Clear(); ignoredTileReasons.Clear();
#if UNITY_EDITOR
        var entry = GenericIdDropdownCache.GetOrBuild(typeof(TileObject), "TileObjects");
        foreach (var (label, id) in entry.Items)
        {
            if (!entry.AssetsById.TryGetValue(id, out var assets) || assets.Count == 0)
            { ignoredTileReasons.Add($"(unknown) id {id}: no asset instance"); continue; }
            var to = assets[0] as TileObject; if (to == null) { ignoredTileReasons.Add($"{label}: not a TileObject"); continue; }

            // Skip rules
            if (id == 0) { ignoredTileReasons.Add($"{to.name} (id 0 ignored)"); continue; }
            var kindStr = to.tileKind.ToString();
            if (string.Equals(kindStr, "Empty", StringComparison.OrdinalIgnoreCase)) { ignoredTileReasons.Add($"{to.name} (tileKind Empty)"); continue; }
            if (to.layer == 0) { ignoredTileReasons.Add($"{to.name} (layer id 0 ignored)"); continue; }

            // Priority: menuSprite -> tileSpawn.sprite -> none (ID label)
            Sprite s = to.menuSprite;
            if (s == null && to.tileSpawn != null)
            {
                var spawnTypeName = to.tileSpawn.spawnType.ToString();
                if (string.Equals(spawnTypeName, "Sprite", StringComparison.OrdinalIgnoreCase)) s = to.tileSpawn.sprite;
            }

            var nm = string.IsNullOrEmpty(to.tileName) ? to.name : to.tileName;
            var ln = Dalichrome.RandomGenerator.TileLayerInfo.GetName(to.layer) ?? $"Layer {to.layer}";
            if (s != null) spriteByTileId[id] = s;
            colorByTileId[id] = to.color;
            tileIdToLayerId[id] = to.layer;
            tileIdToName[id] = nm;
            list.Add(new PaletteItem { id = id, name = nm, sprite = s, color = to.color, layerId = to.layer, layerName = ln });
        }
#endif
        palette = list.OrderBy(p => p.layerName, StringComparer.OrdinalIgnoreCase)
                      .ThenBy(p => p.name, StringComparer.OrdinalIgnoreCase).ToArray();
        currentPaletteIds = palette.Select(p => p.id).ToArray();

        if (asset != null)
        {
            // Always keep data arrays valid
            asset.InitializeIfEmpty(currentLayerIds, currentPaletteIds);

            // --- Auto-rebind to current registries WITHOUT losing data ---
            bool layersMismatch = !asset.ExpectedLayerIds.SequenceEqual(currentLayerIds);
            bool paletteMismatch = !asset.ExpectedPaletteIds.SequenceEqual(currentPaletteIds);
            if (layersMismatch)
            {
                Undo.RecordObject(asset, "Auto Rebind Layers");
                asset.RebindLayersTo(currentLayerIds); // preserves by layerId; adds new as blank
                EditorUtility.SetDirty(asset);
            }
            if (layersMismatch || paletteMismatch)
            {
                Undo.RecordObject(asset, "Update Snapshots");
                asset.UpdateSnapshots(currentLayerIds, currentPaletteIds);
                asset.EnsureTileArrays();
                EditorUtility.SetDirty(asset);
            }
        }

        activeLayerIndex = Mathf.Clamp(activeLayerIndex, 0, Mathf.Max(0, currentLayerIds.Length - 1));
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.MouseMove) Repaint();
        HandleShortcuts();
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSidebar();
            DrawCanvas();
        }
    }

    private int borderStep = 5;

    private void DrawSidebar()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPaneWidth)))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Structure", EditorStyles.boldLabel);
            asset = (Structure)EditorGUILayout.ObjectField(asset, typeof(Structure), false);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Assign or create a Structure to begin.", MessageType.Info);
                if (GUILayout.Button("Create Structure"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Create Structure", "NewStructure", "asset", "Choose save location");
                    if (!string.IsNullOrEmpty(path))
                    {
                        asset = CreateInstance<Structure>();
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        Selection.activeObject = asset;
                        RefreshRegistry();
                    }
                }
                return;
            }

            DrawValidationBanners();

            leftScroll = EditorGUILayout.BeginScrollView(
                leftScroll,
                false,                      
                false,                      
                GUIStyle.none,              
                GUI.skin.verticalScrollbar, 
                GUIStyle.none,              
                GUILayout.Width(LeftPaneWidth),
                GUILayout.ExpandHeight(true)
            );

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                int newW = EditorGUILayout.IntField("Width", asset.width);
                int newH = EditorGUILayout.IntField("Height", asset.height);
                if ((newW != asset.width || newH != asset.height) && GUILayout.Button("Resize", GUILayout.MaxWidth(70)))
                {
                    Undo.RecordObject(asset, "Resize Structure");
                    asset.Resize(newW, newH, Vector2Int.zero);
                    asset.EnsureTileArrays();
                    EditorUtility.SetDirty(asset);
                }
            }

            // Border grow/shrink and collapse-to-bounds
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Canvas Adjust", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                borderStep = EditorGUILayout.IntField("Step", Mathf.Max(1, borderStep));
                if (GUILayout.Button("Collapse to Bounds", GUILayout.Width(160)))
                {
                    Undo.RecordObject(asset, "Collapse to Bounds");
                    asset.CollapseToBounds();
                    EditorUtility.SetDirty(asset);
                }
            }
            // 8 explicit buttons: add/remove around each edge
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Col Left", GUILayout.Width(100))) { Undo.RecordObject(asset, "+ Col Left"); asset.AddColumnsLeft(borderStep); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("+ Row Top", GUILayout.Width(100))) { Undo.RecordObject(asset, "+ Row Top"); asset.AddRowsTop(borderStep); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("+ Col Right", GUILayout.Width(100))) { Undo.RecordObject(asset, "+ Col Right"); asset.AddColumnsRight(borderStep); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("+ Row Bottom", GUILayout.Width(110))) { Undo.RecordObject(asset, "+ Row Bottom"); asset.AddRowsBottom(borderStep); EditorUtility.SetDirty(asset); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("- Col Left", GUILayout.Width(100))) { Undo.RecordObject(asset, "- Col Left"); asset.RemoveColumnsLeft(borderStep); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("- Row Top", GUILayout.Width(100))) { Undo.RecordObject(asset, "- Row Top"); asset.RemoveRowsTop(borderStep); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("- Col Right", GUILayout.Width(100))) { Undo.RecordObject(asset, "- Col Right"); asset.RemoveColumnsRight(borderStep); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("- Row Bottom", GUILayout.Width(110))) { Undo.RecordObject(asset, "- Row Bottom"); asset.RemoveRowsBottom(borderStep); EditorUtility.SetDirty(asset); }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                paletteSearch = EditorGUILayout.TextField("Search", paletteSearch);
                filterByActiveLayer = GUILayout.Toggle(filterByActiveLayer, "Filter by Active Layer", EditorStyles.miniButton, GUILayout.Width(170));
            }
            DrawPaletteGrid();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            for (int i = 0; i < currentLayerIds.Length; i++)
            {
                int lid = currentLayerIds[i]; string lname = (i < currentLayerNames.Length ? currentLayerNames[i] : $"Layer {lid}");
                var L = (asset.Layers as List<Structure.LayerData>)?.Find(x => x.layerId == lid);
                bool vis = L?.visible ?? true;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(activeLayerIndex == i, "", GUILayout.Width(18))) activeLayerIndex = i;
                    GUILayout.Label(lname);
                    GUILayout.FlexibleSpace();
                    bool newVis = GUILayout.Toggle(vis, "Visible", EditorStyles.miniButton, GUILayout.Width(70));
                    if (newVis != vis && L != null) { Undo.RecordObject(asset, "Toggle Layer Visibility"); L.visible = newVis; EditorUtility.SetDirty(asset); }
                    if (GUILayout.Button("Clear", GUILayout.Width(54))) { Undo.RecordObject(asset, "Clear Layer"); asset.ClearLayerById(lid); EditorUtility.SetDirty(asset); }
                }
            }

            EditorGUILayout.Space(8);
            maintenanceFoldout = EditorGUILayout.Foldout(maintenanceFoldout, "Maintenance", true);
            if (maintenanceFoldout)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Clear Unknown Layers")) { Undo.RecordObject(asset, "Clear Unknown Layers"); ClearUnknownLayers(); EditorUtility.SetDirty(asset); }
                    if (GUILayout.Button("Clear Unknown Tile IDs")) { Undo.RecordObject(asset, "Clear Unknown Tile IDs"); ClearUnknownTileIds(); EditorUtility.SetDirty(asset); }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cull Wrong-Layer Tiles")) { Undo.RecordObject(asset, "Cull Wrong-Layer Tiles"); CullTilesWrongLayer(); EditorUtility.SetDirty(asset); }
                    if (GUILayout.Button("Show Ignored Tiles…"))
                    {
                        var msg = ignoredTileReasons.Count == 0 ? "No ignored tiles." : string.Join("\n", ignoredTileReasons.Take(200));
                        EditorUtility.DisplayDialog("Ignored Tiles", msg, "OK");
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            var sep = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(sep.x, sep.y, sep.width, 1), new Color(0, 0, 0, EditorGUIUtility.isProSkin ? 0.5f : 0.2f));

            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(false)))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(tool == ToolMode.Brush, "Brush (B)", EditorStyles.miniButtonLeft)) tool = ToolMode.Brush;
                    if (GUILayout.Toggle(tool == ToolMode.Erase, "Erase (E)", EditorStyles.miniButtonMid)) tool = ToolMode.Erase;
                    if (GUILayout.Toggle(tool == ToolMode.Fill, "Fill (G)", EditorStyles.miniButtonMid)) tool = ToolMode.Fill;
                    if (GUILayout.Toggle(tool == ToolMode.Picker, "Picker (I)", EditorStyles.miniButtonRight)) tool = ToolMode.Picker;
                }
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
                zoom = EditorGUILayout.Slider("Zoom", zoom, MinZoom, MaxZoom);
                otherLayersOpacity = EditorGUILayout.Slider("Other Layers Opacity", Mathf.Clamp(otherLayersOpacity, 0.1f, 1f), 0.1f, 1f);
                if (GUILayout.Button("Reset View")) { zoom = 1f; pan = Vector2.zero; Repaint(); }
            }
        }
    }

    private void DrawPaletteGrid()
    {
        // Filter
        IEnumerable<PaletteItem> items = palette;
        if (filterByActiveLayer && activeLayerIndex >= 0 && activeLayerIndex < currentLayerIds.Length)
        {
            int lid = currentLayerIds[activeLayerIndex];
            items = items.Where(p => p.layerId == lid);
        }
        if (!string.IsNullOrWhiteSpace(paletteSearch))
        {
            var s = paletteSearch.Trim();
            items = items.Where(p => p.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                                   || p.layerName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                                   || p.id.ToString().IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        var arr = items.ToArray();

        int cols = Mathf.Max(1, (int)((LeftPaneWidth - 8) / (PaletteCellSize + PaletteCellPadding)));
        int rows = (arr.Length + cols - 1) / cols;
        float gridW = cols * (PaletteCellSize + PaletteCellPadding) + PaletteCellPadding;
        float gridH = rows * (PaletteCellSize + PaletteCellPadding) + PaletteCellPadding + 8;

        using (var scroll = new EditorGUILayout.ScrollViewScope(paletteScroll, GUILayout.Height(Mathf.Min(300, gridH + 6))))
        {
            paletteScroll = scroll.scrollPosition;
            var area = GUILayoutUtility.GetRect(gridW, gridH, GUILayout.Width(LeftPaneWidth - 16f));
            var start = area.position + new Vector2(PaletteCellPadding, PaletteCellPadding);
            Handles.BeginGUI();

            for (int i = 0; i < arr.Length; i++)
            {
                int c = i % cols; int r = i / cols;
                var rct = new Rect(start.x + c * (PaletteCellSize + PaletteCellPadding), start.y + r * (PaletteCellSize + PaletteCellPadding), PaletteCellSize, PaletteCellSize);
                var p = arr[i];

                bool sel = (paintTileId == p.id);
                var bg = new Color(0, 0, 0, 0.08f);
                EditorGUI.DrawRect(rct, bg);

                // Larger selection outline
                if (sel)
                {
                    var big = rct; big.xMin -= 2; big.yMin -= 2; big.xMax += 2; big.yMax += 2;
                    Handles.DrawSolidRectangleWithOutline(big, Color.clear, new Color(0.2f, 0.6f, 1f, 0.9f));
                }

                var content = new GUIContent("", $"{p.name} \nID: { p.id } \nLayer: { p.layerName}");
                if (GUI.Button(rct, content, GUIStyle.none))
                {
                    paintTileId = p.id;
                    int idx = Array.IndexOf(currentLayerIds, p.layerId);
                    if (idx >= 0) activeLayerIndex = idx; // auto-switch layer
                }

                if (p.sprite != null) DrawSprite(rct, p.sprite, 1f);
                else if (p.color.a > 0f) DrawColor(rct, p.color, 1f);
                else { var prev = GUI.color; GUI.color = new Color(0, 0, 0, 0.85f); GUI.Label(rct, p.id.ToString(), EditorStyles.centeredGreyMiniLabel); GUI.color = prev; }
            }

            Handles.EndGUI();
        }
    }

    private void DrawValidationBanners()
    {
        var snapL = asset.ExpectedLayerIds?.ToArray() ?? Array.Empty<int>();
        var snapP = asset.ExpectedPaletteIds?.ToArray() ?? Array.Empty<int>();
        bool layersMismatch = !Enumerable.SequenceEqual(snapL, currentLayerIds);
        bool paletteMismatch = !Enumerable.SequenceEqual(snapP, currentPaletteIds);

        if (layersMismatch)
        {
            EditorGUILayout.HelpBox("Layer registry changed since save. Rebound automatically on open; you can also force a manual rebind.", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebind Layers to Current Registry"))
                { Undo.RecordObject(asset, "Rebind Layers"); asset.RebindLayersTo(currentLayerIds); asset.UpdateSnapshots(currentLayerIds, currentPaletteIds); asset.EnsureTileArrays(); EditorUtility.SetDirty(asset); }
                if (GUILayout.Button("Update Snapshot Only"))
                { Undo.RecordObject(asset, "Update Snapshots"); asset.UpdateSnapshots(currentLayerIds, currentPaletteIds); EditorUtility.SetDirty(asset); }
            }
        }
        if (paletteMismatch)
        {
            EditorGUILayout.HelpBox("Tiles registry changed since save. Snapshot was refreshed automatically; use this to force it again.", MessageType.Info);
            if (GUILayout.Button("Update Tiles Snapshot"))
            { Undo.RecordObject(asset, "Update Palette Snapshot"); asset.UpdateSnapshots(currentLayerIds, currentPaletteIds); EditorUtility.SetDirty(asset); }
        }
    }

    private void DrawCanvas()
    {
        // Clip grid rendering to its rect so it can't overdraw the sidebar
        var rect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.BeginGroup(rect);
        var localRect = new Rect(0, 0, rect.width, rect.height);

        EditorGUI.DrawRect(localRect, EditorGUIUtility.isProSkin ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.92f, 0.92f, 0.92f));
        if (asset == null) { GUI.EndGroup(); return; }

        float cell = GridCellBase * zoom;
        var gridOrigin = new Vector2(12, 12) + pan; // local inside the group
        var gridW = asset.width * cell; var gridH = asset.height * cell;
        var gridRect = new Rect(gridOrigin.x, gridOrigin.y, gridW, gridH);

        var e = Event.current; // inside group: e.mousePosition is ALREADY local to the group

        if (localRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
        { zoom = Mathf.Clamp(zoom - e.delta.y * 0.05f, MinZoom, MaxZoom); e.Use(); }
        if (localRect.Contains(e.mousePosition) && e.type == EventType.MouseDrag && e.button == 2)
        { pan += e.delta; e.Use(); Repaint(); }

        EditorGUI.DrawRect(gridRect, EditorGUIUtility.isProSkin ? new Color(0.09f, 0.09f, 0.09f) : Color.white);

        Handles.BeginGUI();
        Color line = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0.08f);
        Handles.color = line;
        for (int x = 0; x <= asset.width; x++)
        { float px = gridOrigin.x + x * cell; Handles.DrawLine(new Vector3(px, gridOrigin.y), new Vector3(px, gridOrigin.y + gridH)); }
        for (int y = 0; y <= asset.height; y++)
        { float py = gridOrigin.y + y * cell; Handles.DrawLine(new Vector3(gridOrigin.x, py), new Vector3(gridOrigin.x + gridW, py)); }

        // Draw by layer order with visibility
        for (int li = 0; li < currentLayerIds.Length; li++)
        {
            int lid = currentLayerIds[li];
            var L = (asset.Layers as List<Structure.LayerData>)?.Find(x => x.layerId == lid);
            if (L != null && !L.visible) continue;
            float alpha = (li == activeLayerIndex) ? 1f : Mathf.Clamp(otherLayersOpacity, 0.1f, 1f);
            for (int y = 0; y < asset.height; y++)
                for (int x = 0; x < asset.width; x++)
                {
                    int id = asset.GetTileByLayerId(lid, x, y);
                    if (id < 0) continue;
                    var cellRect = new Rect(gridOrigin.x + x * cell, gridOrigin.y + y * cell, cell, cell); // flush tiles: no ±1 inset
                    if (spriteByTileId.TryGetValue(id, out var sp) && sp != null)
                        DrawSprite(cellRect, sp, alpha);
                    else
                        DrawColor(cellRect, colorByTileId.TryGetValue(id, out var c) ? c : Color.gray, alpha);
                }
        }

        // Hover highlight + invalid-placement X
        var (hx, hy, onGrid) = MouseCell(e.mousePosition, gridOrigin, cell);
        if (onGrid)
        {
            var hilite = new Rect(gridOrigin.x + hx * cell, gridOrigin.y + hy * cell, cell, cell);
            Handles.DrawSolidRectangleWithOutline(hilite, new Color(1, 1, 1, 0.04f), new Color(1, 1, 1, 0.25f));

            // If current selected tile cannot be placed on the active layer, draw a red X
            bool invalid = false;
            if (paintTileId > 0 && tileIdToLayerId.TryGetValue(paintTileId, out var reqLayer))
            {
                int activeLayerId = (activeLayerIndex >= 0 && activeLayerIndex < currentLayerIds.Length) ? currentLayerIds[activeLayerIndex] : -1;
                invalid = (reqLayer != activeLayerId);
            }
            if (invalid)
            {
                Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                Handles.DrawLine(new Vector3(hilite.xMin, hilite.yMin), new Vector3(hilite.xMax, hilite.yMax));
                Handles.DrawLine(new Vector3(hilite.xMin, hilite.yMax), new Vector3(hilite.xMax, hilite.yMin));
            }
        }
        Handles.EndGUI();

        // Hover inspector: per-layer tile IDs & names under cursor (bottom-right)
        if (onGrid)
        {
            var lines = new List<string>();
            lines.Add($"Cell: ({hx},{hy})");
            for (int i = 0; i < currentLayerIds.Length; i++)
            {
                int lid = currentLayerIds[i];
                int id = asset.GetTileByLayerId(lid, hx, hy);
                string lname = (i < currentLayerNames.Length ? currentLayerNames[i] : $"Layer {lid}");
                if (id >= 0)
                {
                    string tname = tileIdToName.TryGetValue(id, out var nm) ? nm : id.ToString();
                    lines.Add($"{lname}: {id} ({tname})");
                }
                else
                {
                    lines.Add($"{lname}: -");
                }
            }
            // Measure simple box
            float w = 0f; foreach (var s in lines) w = Mathf.Max(w, GUI.skin.label.CalcSize(new GUIContent(s)).x);
            float lineH = EditorGUIUtility.singleLineHeight;
            float h = lineH * lines.Count + 8f;

            float boxW = w + 16f, boxH = h;
            // Clamp to keep the box fully visible within the canvas with small margins
            float ix = Mathf.Clamp(localRect.width - (boxW + 8f), 4f, Mathf.Max(4f, localRect.width - boxW - 4f));
            float iy = Mathf.Clamp(localRect.height - (boxH + 8f), 4f, Mathf.Max(4f, localRect.height - boxH - 4f));
            var infoRect = new Rect(ix, iy, boxW, boxH);

            // Background
            EditorGUI.DrawRect(infoRect, new Color(0f, 0f, 0f, 0.65f));
            // Text
            var r = new Rect(infoRect.x + 8f, infoRect.y + 4f, infoRect.width - 12f, lineH);
            foreach (var s in lines) { GUI.Label(r, s, EditorStyles.whiteMiniLabel); r.y += lineH; }
        }

        HandlePainting(localRect, gridOrigin, cell);
        GUI.EndGroup();
    }

    private void HandlePainting(Rect localRect, Vector2 gridOrigin, float cell)
    {
        var e = Event.current; if (asset == null) return; if (!localRect.Contains(e.mousePosition)) return;
        var (cx, cy, onGrid) = MouseCell(e.mousePosition, gridOrigin, cell); if (!onGrid) return;

        int activeLayerId = (activeLayerIndex >= 0 && activeLayerIndex < currentLayerIds.Length) ? currentLayerIds[activeLayerIndex] : -1;
        if (activeLayerId < 0) return;

        // If we have a selected tile, enforce placement layer.
        bool invalidPlacement = false;
        if (paintTileId > 0 && tileIdToLayerId.TryGetValue(paintTileId, out var reqLayer))
            invalidPlacement = (reqLayer != activeLayerId);

        // Change cursor to indicate invalid placement by drawing a red X (already handled in DrawCanvas)
        // Suppress painting for invalid placements (except Picker and Erase which always operate on the active layer)

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isDraggingPaint = true; lastMousePos = e.mousePosition; GUI.FocusControl(null);
            Undo.RecordObject(asset, "Paint Structure");
            if (tool == ToolMode.Picker) paintTileId = asset.GetTileByLayerId(activeLayerId, cx, cy);
            else if (tool == ToolMode.Erase) asset.SetTileByLayerId(activeLayerId, cx, cy, -1);
            else if (!invalidPlacement) ApplyToolAt(activeLayerId, cx, cy);
            asset.EnsureTileArrays(); EditorUtility.SetDirty(asset); Repaint(); e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && isDraggingPaint)
        {
            if ((e.mousePosition - lastMousePos).sqrMagnitude > 0.5f)
            {
                if (tool == ToolMode.Erase) asset.SetTileByLayerId(activeLayerId, cx, cy, -1);
                else if (tool == ToolMode.Picker) paintTileId = asset.GetTileByLayerId(activeLayerId, cx, cy);
                else if (!invalidPlacement) ApplyToolAt(activeLayerId, cx, cy);
                EditorUtility.SetDirty(asset); Repaint(); lastMousePos = e.mousePosition;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        { isDraggingPaint = false; e.Use(); }
    }

    private void ApplyToolAt(int layerId, int x, int y)
    {
        switch (tool)
        {
            case ToolMode.Brush:
                if (paintTileId > 0) asset.SetTileByLayerId(layerId, x, y, paintTileId);
                break;
            case ToolMode.Erase:
                asset.SetTileByLayerId(layerId, x, y, -1);
                break;
            case ToolMode.Fill:
                int target = asset.GetTileByLayerId(layerId, x, y);
                if (target == paintTileId) return;
                FloodFill(layerId, x, y, target, paintTileId);
                break;
            case ToolMode.Picker: break;
        }
    }

    private void FloodFill(int layerId, int sx, int sy, int targetId, int newId)
    {
        if ((uint)sx >= (uint)asset.width || (uint)sy >= (uint)asset.height) return;
        if (targetId == newId) return;
        var q = new Queue<Vector2Int>(256); q.Enqueue(new Vector2Int(sx, sy));
        while (q.Count > 0)
        {
            var p = q.Dequeue(); int x = p.x, y = p.y;
            if ((uint)x >= (uint)asset.width || (uint)y >= (uint)asset.height) continue;
            if (asset.GetTileByLayerId(layerId, x, y) != targetId) continue;
            asset.SetTileByLayerId(layerId, x, y, newId);
            q.Enqueue(new Vector2Int(x + 1, y)); q.Enqueue(new Vector2Int(x - 1, y)); q.Enqueue(new Vector2Int(x, y + 1)); q.Enqueue(new Vector2Int(x, y - 1));
        }
    }

    private void HandleShortcuts()
    {
        var e = Event.current; if (e.type != EventType.KeyDown) return;
        switch (e.keyCode)
        { case KeyCode.B: tool = ToolMode.Brush; Repaint(); break; case KeyCode.E: tool = ToolMode.Erase; Repaint(); break; case KeyCode.G: tool = ToolMode.Fill; Repaint(); break; case KeyCode.I: tool = ToolMode.Picker; Repaint(); break; }
    }

    private (int x, int y, bool onGrid) MouseCell(Vector2 mouse, Vector2 origin, float cell)
    { int x = Mathf.FloorToInt((mouse.x - origin.x) / cell); int y = Mathf.FloorToInt((mouse.y - origin.y) / cell); bool on = x >= 0 && x < asset.width && y >= 0 && y < asset.height; return (x, y, on); }

    private static void DrawSprite(Rect r, Sprite s, float a)
    {
        if (s == null || s.texture == null) return;
        var tex = s.texture; var tr = s.textureRect;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        var prev = GUI.color; GUI.color = new Color(1, 1, 1, a);
        GUI.DrawTextureWithTexCoords(r, tex, uv, true);
        GUI.color = prev;
    }

    private static void DrawColor(Rect r, Color c, float a)
    { var cc = c; if (cc.a <= 0f) cc = new Color(0.6f, 0.6f, 0.6f, 1f); cc.a = a; EditorGUI.DrawRect(r, cc); }

    // --- Maintenance helpers ---
    private void ClearUnknownLayers()
    {
        var valid = new HashSet<int>(currentLayerIds);
        foreach (var L in asset.Layers)
        {
            if (!valid.Contains(L.layerId) || L.layerId == 0)
            {
                if (L.tiles == null) continue;
                for (int i = 0; i < L.tiles.Length; i++) L.tiles[i] = -1;
            }
        }
    }

    private void ClearUnknownTileIds()
    {
        var valid = new HashSet<int>(currentPaletteIds);
        foreach (var L in asset.Layers)
        {
            if (L.tiles == null) continue;
            for (int i = 0; i < L.tiles.Length; i++)
            {
                int id = L.tiles[i];
                if (id <= 0) continue; // ignore 0 and empties
                if (!valid.Contains(id)) L.tiles[i] = -1;
            }
        }
    }

    private void CullTilesWrongLayer()
    {
        // Remove any tile whose configured layer doesn't match the layer it resides on (skip <=0)
        foreach (var L in asset.Layers)
        {
            int layerId = L.layerId;
            if (L.tiles == null) continue;
            for (int i = 0; i<L.tiles.Length; i++)
            {
                int id = L.tiles[i];
                if (id <= 0) continue; // keep 0/empty
               if (!tileIdToLayerId.TryGetValue(id, out var reqLayer) || reqLayer != layerId)
                   L.tiles[i] = -1;
            }
        }
    }
}
#endif
