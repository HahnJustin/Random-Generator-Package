// ==============================
// File: StructureEditorWindow.cs
// ================================
// Editor window: registry-driven, uses TileObject sprites, shows layer names,
// ignores layerId==0, tileObject id==0, and TileObjects with tileKind==Empty.
// Auto-switches to a tile's configured layer when that tile is selected.
// Tiles palette is a grid with search + tooltips. Double-click StructureObject opens this.
// Grid is clipped so it won't overdraw the sidebar. Auto-rebinds registries on open.

#if UNITY_EDITOR
using Dalichrome.RandomGenerator.UserData;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class StructureEditorWindow : EditorWindow
{
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 8f;
    private const float GridCellBase = 32f;
    private const float LeftPaneWidth = 440f; // narrower to avoid horizontal scroll

    [SerializeField] private StructureObject asset;

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

    // Meta-mode (switch tools to operate on metadata instead of tiles)
    private bool _metaMode = false;

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

    // ---- Mask tile (synthetic palette entry) ----
    private const int MaskTileId = -1;
    private const string MaskTileName = "Inherit (Structure Mask)";
    private const string MaskSpriteFile = "StructureMask.png";
    private static Sprite s_maskSprite;
    private static bool s_maskTried;
    private static string s_scriptFolder; // cached script folder to avoid repeated scans

    // --- Live metadata editing (debounced) ---
    private double _metaLastEditTime = -1;
    private bool _metaDirty = false;
    private bool _metaUndoArmed = false;
    private int _metaUndoGroup = -1;
    private const string MetaTextControlName = "MetaTextArea";

    // How long after the last keystroke we commit to the asset
    private const double MetaCommitDelay = 0.20; // 200ms feels snappy

    // --- NEW: metadata icon sprites ---
    private const string MetaIconFile = "MetaDataIcon.png";
    private const string MetaSelectionFile = "MetaSelection.png";

    private static Sprite s_metaIconSprite;
    private static Sprite s_metaSelectionSprite;
    private static bool s_metaIconTried;
    private static bool s_metaSelectionTried;

    // --- Hover cache (perf) ---
    private Vector2Int _hoverCell = new Vector2Int(int.MinValue, int.MinValue);
    private string[] _hoverLines = Array.Empty<string>();
    private float _hoverBoxW = 0f, _hoverBoxH = 0f;
    private int _hoverActiveLayerIndex = -1; // so we refresh when active layer changes
    private readonly GUIContent _scratchContent = new GUIContent(); // reuse to avoid allocs
    private bool _hoverDirty = true; // NEW: force rebuild when data changes

    // --- Metadata state ---
    // Current "target" cell for metadata editing (x,y,layerId)
    private Vector3Int _metaTarget = new Vector3Int(-1, -1, -1);
    private string _metaText = "";

    // Source cell for metadata copy (Picker in meta mode)
    private Vector3Int _metaSource = new Vector3Int(-1, -1, -1);

    // Visual toggle for metadata markers
    private bool _showMetaMarkers = true;

    [MenuItem("Random Generator/Structure Painter")]
    public static void Open()
    {
        var w = GetWindow<StructureEditorWindow>("Structure Painter");
        EnsureGoodInitialSize(w);
        w.Show();
    }
    public static void Open(StructureObject a)
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
        var obj = EditorUtility.InstanceIDToObject(instanceID) as StructureObject;
        if (obj != null) { Open(obj); return true; }
        return false;
    }

    private void OnEnable() { wantsMouseMove = true; RefreshRegistry(); }
    private void OnFocus() { RefreshRegistry(); Repaint(); }

    private void RefreshRegistry()
    {
        // LAYERS: from TileLayerRegistry, filter out 0
        var ids = Dalichrome.RandomGenerator.TileLayerRegistry.AllLayerIds?
            .Where(l => l != 0)
            .ToArray() ?? Array.Empty<int>();

        Array.Sort(ids);            // ensure ascending by layerId
        currentLayerIds = ids;

        currentLayerNames = currentLayerIds.Select(lid => Dalichrome.RandomGenerator.TileLayerRegistry.GetName(lid) ?? $"Layer {lid}").ToArray();

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
            var ln = Dalichrome.RandomGenerator.TileLayerRegistry.GetName(to.layer) ?? $"Layer {to.layer}";
            if (s != null) spriteByTileId[id] = s;
            colorByTileId[id] = to.color;
            tileIdToLayerId[id] = to.layer;
            tileIdToName[id] = nm;
            list.Add(new PaletteItem { id = id, name = nm, sprite = s, color = to.color, layerId = to.layer, layerName = ln });
        }

        // Add synthetic -1 mask (always available; “layerName” says any layer)
        s_maskSprite = EnsureMaskSprite();
        list.Insert(0, new PaletteItem
        {
            id = MaskTileId,
            name = MaskTileName,
            sprite = s_maskSprite,
            color = new Color(0.2f, 0.2f, 0.2f, 1f), // fallback if sprite missing
            layerId = -1,
            layerName = "Any Layer"
        });

#endif
        palette = list.OrderBy(p => p.id == MaskTileId ? -1 : 0)
                    .ThenBy(p => p.layerName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(p => p.name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
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

    private static void EnsureScriptFolder()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(s_scriptFolder)) return;

        var guids = AssetDatabase.FindAssets("t:MonoScript StructureEditorWindow");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms != null && ms.GetClass() == typeof(StructureEditorWindow))
            {
                s_scriptFolder = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
                break;
            }
        }
#endif
    }

    // existing mask loader can now reuse EnsureScriptFolder()
    private static Sprite EnsureMaskSprite()
    {
#if UNITY_EDITOR
        if (s_maskSprite != null || s_maskTried) return s_maskSprite;

        EnsureScriptFolder();

        if (!string.IsNullOrEmpty(s_scriptFolder))
        {
            var maskPath = (s_scriptFolder + "/" + MaskSpriteFile).Replace("\\", "/");
            s_maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(maskPath);
        }

        s_maskTried = true;
        return s_maskSprite;
#else
    return null;
#endif
    }

    // NEW: metadata icon (for “cell has metadata”)
    private static Sprite EnsureMetaIconSprite()
    {
#if UNITY_EDITOR
        if (s_metaIconSprite != null || s_metaIconTried) return s_metaIconSprite;

        EnsureScriptFolder();

        if (!string.IsNullOrEmpty(s_scriptFolder))
        {
            var path = (s_scriptFolder + "/" + MetaIconFile).Replace("\\", "/");
            s_metaIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        s_metaIconTried = true;
        return s_metaIconSprite;
#else
    return null;
#endif
    }

    // NEW: metadata selection icon (for picked meta source)
    private static Sprite EnsureMetaSelectionSprite()
    {
#if UNITY_EDITOR
        if (s_metaSelectionSprite != null || s_metaSelectionTried) return s_metaSelectionSprite;

        EnsureScriptFolder();

        if (!string.IsNullOrEmpty(s_scriptFolder))
        {
            var path = (s_scriptFolder + "/" + MetaSelectionFile).Replace("\\", "/");
            s_metaSelectionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        s_metaSelectionTried = true;
        return s_metaSelectionSprite;
#else
    return null;
#endif
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

        // If we were typing and lost focus, commit immediately
        if (_metaDirty)
        {
            bool focused = GUI.GetNameOfFocusedControl() == MetaTextControlName;
            if (!focused && !EditorGUIUtility.editingTextField)
                CommitMetaIfDirty(force: true);
        }
    }

    private int borderStep = 5;

    private void DrawSidebar()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPaneWidth)))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Structure", EditorStyles.boldLabel);
            asset = (StructureObject)EditorGUILayout.ObjectField(asset, typeof(StructureObject), false);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Assign or create a Structure to begin.", MessageType.Info);
                if (GUILayout.Button("Create Structure"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Create Structure", "NewStructure", "asset", "Choose save location");
                    if (!string.IsNullOrEmpty(path))
                    {
                        asset = CreateInstance<StructureObject>();
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

            if (!_metaMode)
            {
                // Normal tile mode: full palette UI
                EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    paletteSearch = EditorGUILayout.TextField("Search", paletteSearch);
                    filterByActiveLayer = GUILayout.Toggle(
                        filterByActiveLayer,
                        "Filter by Active Layer",
                        EditorStyles.miniButton,
                        GUILayout.Width(170)
                    );
                }
                DrawPaletteGrid();
            }
            else
            {
                // Meta mode: hide palette and show an obvious banner instead
                EditorGUILayout.LabelField("Tiles (disabled in Metadata Mode)", EditorStyles.boldLabel);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

            // Layer visibility shortcuts
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Solo Active", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                {
                    if (asset != null && currentLayerIds.Length > 0 && activeLayerIndex >= 0 && activeLayerIndex < currentLayerIds.Length)
                    {
                        int activeLid = currentLayerIds[activeLayerIndex];
                        Undo.RecordObject(asset, "Solo Active Layer");
                        foreach (var L in asset.Layers)
                        {
                            L.visible = (L.layerId == activeLid);
                        }
                        EditorUtility.SetDirty(asset);
                        Repaint();
                    }
                }
                if (GUILayout.Button("Show All", EditorStyles.miniButtonRight, GUILayout.Width(80)))
                {
                    if (asset != null)
                    {
                        Undo.RecordObject(asset, "Show All Layers");
                        foreach (var L in asset.Layers)
                        {
                            L.visible = true;
                        }
                        EditorUtility.SetDirty(asset);
                        Repaint();
                    }
                }
            }

            for (int i = 0; i < currentLayerIds.Length; i++)
            {
                int lid = currentLayerIds[i]; string lname = (i < currentLayerNames.Length ? currentLayerNames[i] : $"Layer {lid}");
                var L = (asset.Layers as List<StructureObject.LayerData>)?.Find(x => x.layerId == lid);
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
            DrawMetadataPanel();

            EditorGUILayout.Space(8);
            maintenanceFoldout = EditorGUILayout.Foldout(maintenanceFoldout, "Maintenance", true);
            if (maintenanceFoldout)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Clear Unknown Layers"))
                        {
                            Undo.RecordObject(asset, "Clear Unknown Layers");
                            ClearUnknownLayers();
                            EditorUtility.SetDirty(asset);
                        }
                        if (GUILayout.Button("Clear Unknown Tile IDs"))
                        {
                            Undo.RecordObject(asset, "Clear Unknown Tile IDs");
                            ClearUnknownTileIds();
                            EditorUtility.SetDirty(asset);
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Cull Wrong-Layer Tiles"))
                        {
                            Undo.RecordObject(asset, "Cull Wrong-Layer Tiles");
                            CullTilesWrongLayer();
                            EditorUtility.SetDirty(asset);
                        }
                        if (GUILayout.Button("Show Ignored Tiles…"))
                        {
                            var msg = ignoredTileReasons.Count == 0 ? "No ignored tiles." : string.Join("\n", ignoredTileReasons.Take(200));
                            EditorUtility.DisplayDialog("Ignored Tiles", msg, "OK");
                        }
                        if (GUILayout.Button("Clear Tiles"))
                        {
                            Undo.RecordObject(asset, "Clear Tiles");
                            asset.SetAllTiles(0);
                            asset.EnsureTileArrays();
                            EditorUtility.SetDirty(asset);
                        }
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

                bool newMetaMode = EditorGUILayout.ToggleLeft("Metadata Mode (M)", _metaMode);
                if (newMetaMode != _metaMode)
                {
                    _metaMode = newMetaMode;
                    if (!_metaMode)
                    {
                        // Same behavior as keyboard toggle
                        _metaTarget = new Vector3Int(-1, -1, -1);
                        _metaSource = new Vector3Int(-1, -1, -1);
                        _metaText = string.Empty;
                        GUI.FocusControl(null);
                    }
                    Repaint();
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
                zoom = EditorGUILayout.Slider("Zoom", zoom, MinZoom, MaxZoom);
                otherLayersOpacity = EditorGUILayout.Slider("Other Layers Opacity", Mathf.Clamp(otherLayersOpacity, 0.1f, 1f), 0.1f, 1f);
                if (GUILayout.Button("Reset View")) { zoom = 1f; pan = Vector2.zero; Repaint(); }
            }
        }
    }

    private void DrawMetadataPanel()
    {
        EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);

        if (asset == null)
        {
            EditorGUILayout.HelpBox("Assign a Structure to edit metadata.", MessageType.Info);
            return;
        }

        _showMetaMarkers = EditorGUILayout.ToggleLeft("Show metadata borders", _showMetaMarkers);

        EditorGUILayout.LabelField(
            "Meta Mode: when enabled, Brush/Fill/Erase/Picker operate on metadata instead of tiles. " +
            "Use Picker (I) to select a tile. If it already has metadata, that tile becomes the copy source " +
            "for Brush/Fill. If it has no metadata, you can type new metadata here, but Brush/Fill " +
            "won't work until you've picked a tile that already has metadata.",
            EditorStyles.wordWrappedMiniLabel);

        if (_metaTarget.x < 0 || _metaTarget.y < 0 || _metaTarget.z <= 0)
        {
            EditorGUILayout.HelpBox(
                "Enable Metadata Mode (M), select Picker (I), then click a tile that already has metadata.\n" +
                "That tile becomes the source for Brush/Fill and is editable here.",
                MessageType.Info);
            return;
        }

        // Fetch tile ID at target
        int lid = _metaTarget.z;
        int tx = _metaTarget.x;
        int ty = _metaTarget.y;

        string layerName = "Unknown Layer";
        int layerIndex = Array.IndexOf(currentLayerIds, lid);
        if (layerIndex >= 0 && layerIndex < currentLayerNames.Length)
            layerName = currentLayerNames[layerIndex];

        int tileId = asset.GetTileByLayerId(lid, tx, ty);
        string tileName =
            tileId == 0 ? "(empty)" :
            tileId == MaskTileId ? MaskTileName :
            (tileIdToName.TryGetValue(tileId, out var nm) ? nm : $"ID {tileId}");

        // Display nicer header
        EditorGUILayout.LabelField(
            $"Target: ({tx}, {ty}) on {layerName}");

        // Display tile info
        EditorGUILayout.LabelField(
            $"Tile: {tileName} (id {tileId})",
            EditorStyles.miniLabel);

        EditorGUILayout.LabelField(
            "Raw metadata (e.g. field:1, other:2, ...)",
            EditorStyles.miniLabel);


        GUI.SetNextControlName(MetaTextControlName);

        EditorGUI.BeginChangeCheck();
        string newText = EditorGUILayout.TextArea(_metaText, GUILayout.MinHeight(40));
        if (EditorGUI.EndChangeCheck())
        {
            // User typed
            _metaText = newText;

            BeginMetaTypingUndoIfNeeded();
            _metaDirty = true;
            _metaLastEditTime = EditorApplication.timeSinceStartup;

            // Keep UI responsive
            _hoverDirty = true;
            Repaint();
        }

        // Commit debounced while typing
        CommitMetaIfDirty(force: false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear"))
            {
                BeginMetaTypingUndoIfNeeded();
                _metaText = string.Empty;
                _metaDirty = true;
                _metaLastEditTime = EditorApplication.timeSinceStartup;

                // Force immediate commit so Clear feels instant
                CommitMetaIfDirty(force: true);

                GUI.FocusControl(null);
                Repaint();
            }

            if (GUILayout.Button("Unselect", GUILayout.Width(80)))
            {
                // Commit any pending edits before leaving
                CommitMetaIfDirty(force: true);

                _metaTarget = new Vector3Int(-1, -1, -1);
                _metaSource = new Vector3Int(-1, -1, -1);
                _metaText = string.Empty;

                CancelMetaTypingSession();

                _hoverDirty = true;
                GUI.FocusControl(null);
                Repaint();
            }
        }
    }

    private void RebuildHoverCacheIfNeeded(int hx, int hy, Rect localRect)
    {
        if (asset == null) return;

        // If cell or active layer *and* nothing changed, keep cache
        if (!_hoverDirty &&
            _hoverCell.x == hx && _hoverCell.y == hy &&
            _hoverActiveLayerIndex == activeLayerIndex)
            return;

        _hoverCell = new Vector2Int(hx, hy);
        _hoverActiveLayerIndex = activeLayerIndex;
        _hoverDirty = false; // we'll rebuild now

        // Build lines without per-frame GC
        var linesList = new List<string>(1 + currentLayerIds.Length);
        linesList.Add($"Cell: ({hx},{hy})");

        for (int i = 0; i < currentLayerIds.Length; i++)
        {
            int lid = currentLayerIds[i];
            int id = asset.GetTileByLayerId(lid, hx, hy);
            string lname = (i < currentLayerNames.Length ? currentLayerNames[i] : $"Layer {lid}");

            if (id > 0)
            {
                string tname = tileIdToName.TryGetValue(id, out var nm) ? nm : id.ToString();
                linesList.Add($"{lname}: {id} ({tname})");
            }
            else if (id == MaskTileId)
            {
                linesList.Add($"{lname}: {MaskTileId} ({MaskTileName})");
            }
            else
            {
                linesList.Add($"{lname}: -");
            }

            string meta = asset.GetMetadata(lid, hx, hy);
            if (!string.IsNullOrEmpty(meta))
            {
                linesList.Add($"    meta: {meta}");
            }
        }

        _hoverLines = linesList.ToArray();

        // Measure text once
        var style = EditorStyles.whiteMiniLabel;
        float w = 0f;
        for (int i = 0; i < _hoverLines.Length; i++)
        {
            _scratchContent.text = _hoverLines[i];
            var size = style.CalcSize(_scratchContent);
            if (size.x > w) w = size.x;
        }

        float lineH = EditorGUIUtility.singleLineHeight;
        float h = lineH * _hoverLines.Length + 8f;

        _hoverBoxW = w + 16f;
        _hoverBoxH = h;
    }


    private void DrawPaletteGrid()
    {
        // Filter
        IEnumerable<PaletteItem> items = palette;
        if (filterByActiveLayer && activeLayerIndex >= 0 && activeLayerIndex < currentLayerIds.Length)
        {
            int lid = currentLayerIds[activeLayerIndex];
            items = items.Where(p => p.id == MaskTileId || p.layerId == lid);
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

                if (sel)
                {
                    var big = rct; big.xMin -= 2; big.yMin -= 2; big.xMax += 2; big.yMax += 2;
                    Handles.DrawSolidRectangleWithOutline(big, Color.clear, new Color(0.2f, 0.6f, 1f, 0.9f));
                }

                var content = new GUIContent("", $"{p.name} \nID: {p.id} \nLayer: {p.layerName}");
                if (GUI.Button(rct, content, GUIStyle.none))
                {
                    paintTileId = p.id;
                    int idx = Array.IndexOf(currentLayerIds, p.layerId);
                    if (idx >= 0) activeLayerIndex = idx; // auto-switch layer
                }

                if (p.sprite != null) DrawSprite(rct, p.sprite, 1f);
                else if (p.color.a > 0f) DrawColor(rct, p.color, 1f);
                else
                {
                    var prev = GUI.color;
                    GUI.color = new Color(0, 0, 0, 0.85f);
                    GUI.Label(rct, p.id.ToString(), EditorStyles.centeredGreyMiniLabel);
                    GUI.color = prev;
                }
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
        // Ensure sprites are available
        s_maskSprite = EnsureMaskSprite();
        s_metaIconSprite = EnsureMetaIconSprite();
        s_metaSelectionSprite = EnsureMetaSelectionSprite();

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

        // *** PERF: compute visible cell bounds and draw only that region ***
        int minX = 0, minY = 0, maxX = asset.width - 1, maxY = asset.height - 1;
        if (cell > 0.0001f)
        {
            float visX0 = Mathf.Max(localRect.xMin, gridRect.xMin);
            float visY0 = Mathf.Max(localRect.yMin, gridRect.yMin);
            float visX1 = Mathf.Min(localRect.xMax, gridRect.xMax);
            float visY1 = Mathf.Min(localRect.yMax, gridRect.yMax);

            if (visX1 > visX0 && visY1 > visY0)
            {
                minX = Mathf.Clamp(Mathf.FloorToInt((visX0 - gridOrigin.x) / cell), 0, asset.width - 1);
                maxX = Mathf.Clamp(Mathf.CeilToInt((visX1 - gridOrigin.x) / cell), 0, asset.width) - 1;
                minY = Mathf.Clamp(Mathf.FloorToInt((visY0 - gridOrigin.y) / cell), 0, asset.height - 1);
                maxY = Mathf.Clamp(Mathf.CeilToInt((visY1 - gridOrigin.y) / cell), 0, asset.height) - 1;
            }
        }

        Handles.BeginGUI();
        Color line = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0.08f);
        Handles.color = line;

        // Grid lines (only in visible bounds)
        for (int x = minX; x <= maxX + 1; x++)
        {
            float px = gridOrigin.x + x * cell; Handles.DrawLine(new Vector3(px, gridOrigin.y + minY * cell), new Vector3(px, gridOrigin.y + (maxY + 1) * cell));
        }
        for (int y = minY; y <= maxY + 1; y++)
        {
            float py = gridOrigin.y + y * cell; Handles.DrawLine(new Vector3(gridOrigin.x + minX * cell, py), new Vector3(gridOrigin.x + (maxX + 1) * cell, py));
        }

        // Draw by layer order with visibility (only visible cells)
        for (int li = 0; li < currentLayerIds.Length; li++)
        {
            int lid = currentLayerIds[li];
            var L = (asset.Layers as List<StructureObject.LayerData>)?.Find(x => x.layerId == lid);
            if (L != null && !L.visible) continue;
            float alpha = (li == activeLayerIndex) ? 1f : Mathf.Clamp(otherLayersOpacity, 0.1f, 1f);

            for (int y = minY; y <= maxY; y++)
            {
                float rowY = gridOrigin.y + y * cell;
                for (int x = minX; x <= maxX; x++)
                {
                    int id = asset.GetTileByLayerId(lid, x, y);
                    if (id == 0) continue; // Empty

                    var cellRect = new Rect(gridOrigin.x + x * cell, rowY, cell, cell);

                    // StructureObject Mask 
                    if (id == MaskTileId)
                    {
                        if (s_maskSprite != null) DrawSprite(cellRect, s_maskSprite, alpha);
                        else EditorGUI.DrawRect(cellRect, new Color(1f, 0f, 1f, 0.25f)); // fallback only when no sprite
                    }
                    else
                    {
                        if (spriteByTileId.TryGetValue(id, out var sp) && sp != null)
                            DrawSprite(cellRect, sp, alpha);
                        else
                            DrawColor(cellRect, colorByTileId.TryGetValue(id, out var c) ? c : Color.gray, alpha);
                    }

                    // Metadata icon marker
                    string metaHere = asset.GetMetadata(lid, x, y);
                    if (_showMetaMarkers && !string.IsNullOrEmpty(metaHere))
                    {
                        float iconAlpha = (li == activeLayerIndex) ? alpha : alpha * 0.5f;

                        if (s_metaIconSprite != null)
                        {
                            // Slightly inset so we don't bleed into grid lines
                            var iconRect = new Rect(
                                cellRect.x + 2f,
                                cellRect.y + 2f,
                                cellRect.width - 4f,
                                cellRect.height - 4f
                            );
                            DrawSprite(iconRect, s_metaIconSprite, iconAlpha);
                        }
                        else
                        {
                            // Fallback: keep the yellow border if icon is missing
                            var borderRect = new Rect(
                                cellRect.x + 2f,
                                cellRect.y + 2f,
                                cellRect.width - 4f,
                                cellRect.height - 4f
                            );
                            var borderColor = new Color(1.0f, 0.85f, 0.2f, iconAlpha);
                            Handles.DrawSolidRectangleWithOutline(borderRect, Color.clear, borderColor);
                        }
                    }

                    // Highlight currently selected metadata TARGET (even if it has no metadata)
                    if (_showMetaMarkers && _metaMode &&
                        _metaTarget.z == lid &&
                        _metaTarget.x == x &&
                        _metaTarget.y == y)
                    {
                        var tgtRect = new Rect(
                            cellRect.x + 1f,
                            cellRect.y + 1f,
                            cellRect.width - 2f,
                            cellRect.height - 2f
                        );

                        // White outline so it's obvious what is selected
                        var tgtColor = new Color(1f, 1f, 1f, 0.95f);
                        Handles.DrawSolidRectangleWithOutline(tgtRect, Color.clear, tgtColor);
                    }

                    // Highlight current metadata source (picked via Picker in meta mode)
                    if (_showMetaMarkers && _metaMode &&
                        _metaSource.z == lid &&
                        _metaSource.x == x &&
                        _metaSource.y == y)
                    {
                        float selAlpha = 1f;

                        if (s_metaSelectionSprite != null)
                        {
                            // Full cell overlay for clear visibility
                            DrawSprite(cellRect, s_metaSelectionSprite, selAlpha);
                        }
                        else
                        {
                            // Fallback: bright border
                            var srcRect = new Rect(
                                cellRect.x + 1f,
                                cellRect.y + 1f,
                                cellRect.width - 2f,
                                cellRect.height - 2f
                            );
                            var srcColor = new Color(1f, 1f, 0.3f, 1f);
                            Handles.DrawSolidRectangleWithOutline(srcRect, Color.clear, srcColor);
                        }
                    }
                }
            }
        }

        // Hover highlight + invalid-placement X (tile placement only)
        var (hx, hy, onGrid) = MouseCell(e.mousePosition, gridOrigin, cell);
        if (onGrid)
        {
            var hilite = new Rect(gridOrigin.x + hx * cell, gridOrigin.y + hy * cell, cell, cell);
            Handles.DrawSolidRectangleWithOutline(hilite, new Color(1, 1, 1, 0.04f), new Color(1, 1, 1, 0.25f));

            // If current selected tile cannot be placed on the active layer, draw a red X
            bool invalid = false;
            if (!_metaMode && paintTileId > 0 && tileIdToLayerId.TryGetValue(paintTileId, out var reqLayer))
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

        // Hover inspector (repaint-only; cached for perf)
        if (onGrid && Event.current.type == EventType.Repaint)
        {
            RebuildHoverCacheIfNeeded(hx, hy, localRect);

            float boxW = _hoverBoxW;
            float boxH = _hoverBoxH;
            float ix = Mathf.Clamp(localRect.width - (boxW + 8f), 4f, Mathf.Max(4f, localRect.width - boxW - 4f));
            float iy = Mathf.Clamp(localRect.height - (boxH + 8f), 4f, Mathf.Max(4f, localRect.height - boxH - 4f));
            var infoRect = new Rect(ix, iy, boxW, boxH);

            EditorGUI.DrawRect(infoRect, new Color(0f, 0f, 0f, 0.65f));

            var style = EditorStyles.whiteMiniLabel;
            float lineH = EditorGUIUtility.singleLineHeight;
            var r = new Rect(infoRect.x + 8f, infoRect.y + 4f, infoRect.width - 12f, lineH);

            for (int i = 0; i < _hoverLines.Length; i++)
            {
                _scratchContent.text = _hoverLines[i];
                GUI.Label(r, _scratchContent, style);
                r.y += lineH;
            }
        }

        HandlePainting(localRect, gridOrigin, cell);
        GUI.EndGroup();
    }

    private void HandlePainting(Rect localRect, Vector2 gridOrigin, float cell)
    {
        var e = Event.current;
        if (asset == null) return;
        if (!localRect.Contains(e.mousePosition)) return;

        var (cx, cy, onGrid) = MouseCell(e.mousePosition, gridOrigin, cell);
        if (!onGrid) return;

        int activeLayerId = (activeLayerIndex >= 0 && activeLayerIndex < currentLayerIds.Length)
            ? currentLayerIds[activeLayerIndex]
            : -1;
        if (activeLayerId < 0) return;

        // ===========================
        // META MODE: ONLY METADATA
        // ===========================
        if (_metaMode)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                GUI.FocusControl(null);
                Undo.RecordObject(asset, "Edit Metadata");

                if (tool == ToolMode.Picker)
                {
                    // Pick target + optional source
                    string existing = asset.GetMetadata(activeLayerId, cx, cy);
                    var picked = new Vector3Int(cx, cy, activeLayerId);

                    // Commit any pending edits on previous target before switching selection
                    CommitMetaIfDirty(force: true);
                    CancelMetaTypingSession();

                    _metaTarget = picked;

                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        // This tile has metadata → source + editable target
                        _metaSource = picked;
                        _metaText = existing;
                    }
                    else
                    {
                        // No metadata: editable target only, no source for Brush/Fill yet
                        _metaSource = new Vector3Int(-1, -1, -1);
                        _metaText = string.Empty;
                    }
                }
                else if (tool == ToolMode.Brush && MetaSourceIsValid())
                {
                    // Single-click brush also writes
                    string src = asset.GetMetadata(_metaSource.z, _metaSource.x, _metaSource.y);
                    if (!string.IsNullOrWhiteSpace(src))
                    {
                        asset.SetMetadata(activeLayerId, cx, cy, src);
                    }
                }
                else if (tool == ToolMode.Erase)
                {
                    asset.SetMetadata(activeLayerId, cx, cy, null);
                }
                else if (tool == ToolMode.Fill && MetaSourceIsValid())
                {
                    // Flood fill metadata from clicked cell target → src text
                    string src = asset.GetMetadata(_metaSource.z, _metaSource.x, _metaSource.y);
                    if (!string.IsNullOrWhiteSpace(src))
                    {
                        string targetRaw = asset.GetMetadata(activeLayerId, cx, cy);
                        FloodFillMetadata(activeLayerId, cx, cy, targetRaw, src);
                    }
                }

                EditorUtility.SetDirty(asset);
                _hoverDirty = true;
                Repaint();

                // Drag only for Brush/Erase in meta mode
                isDraggingPaint = (tool == ToolMode.Brush || tool == ToolMode.Erase);
                lastMousePos = e.mousePosition;

                e.Use();
                return; // IMPORTANT: do NOT fall through into tile code
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && isDraggingPaint)
            {
                if ((e.mousePosition - lastMousePos).sqrMagnitude > 0.5f)
                {
                    Undo.RecordObject(asset, "Edit Metadata");

                    if (tool == ToolMode.Brush && MetaSourceIsValid())
                    {
                        string src = asset.GetMetadata(_metaSource.z, _metaSource.x, _metaSource.y);
                        if (!string.IsNullOrWhiteSpace(src))
                        {
                            asset.SetMetadata(activeLayerId, cx, cy, src);
                        }
                    }
                    else if (tool == ToolMode.Erase)
                    {
                        asset.SetMetadata(activeLayerId, cx, cy, null);
                    }

                    EditorUtility.SetDirty(asset);
                    _hoverDirty = true;
                    Repaint();
                    lastMousePos = e.mousePosition;
                }

                e.Use();
                return;
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDraggingPaint = false;
                e.Use();
                return;
            }

            // In meta mode, do absolutely nothing to tiles.
            return;
        }

        // ===========================
        // TILE MODE: ONLY TILES
        // ===========================

        // If we have a selected tile, enforce placement layer (for tile painting only).
        bool invalidPlacement = false;
        if (paintTileId > 0 && tileIdToLayerId.TryGetValue(paintTileId, out var reqLayer))
            invalidPlacement = (reqLayer != activeLayerId);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            GUI.FocusControl(null);
            Undo.RecordObject(asset, "Paint Structure");

            isDraggingPaint = true;
            lastMousePos = e.mousePosition;

            if (tool == ToolMode.Picker)
            {
                // Eyedropper: sample tile ID, no metadata touched
                paintTileId = asset.GetTileByLayerId(activeLayerId, cx, cy);
            }
            else if (tool == ToolMode.Erase)
            {
                asset.SetTileByLayerId(activeLayerId, cx, cy, 0);
                _hoverDirty = true;
            }
            else if (!invalidPlacement)
            {
                ApplyToolAt(activeLayerId, cx, cy); // Brush / Fill
                _hoverDirty = true;
            }

            asset.EnsureTileArrays();
            EditorUtility.SetDirty(asset);
            Repaint();
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && isDraggingPaint)
        {
            if ((e.mousePosition - lastMousePos).sqrMagnitude > 0.5f)
            {
                Undo.RecordObject(asset, "Paint Structure");

                if (tool == ToolMode.Erase)
                {
                    asset.SetTileByLayerId(activeLayerId, cx, cy, 0);
                    _hoverDirty = true;
                }
                else if (tool == ToolMode.Picker)
                {
                    // Optional: eyedropper while dragging
                    paintTileId = asset.GetTileByLayerId(activeLayerId, cx, cy);
                }
                else if (!invalidPlacement)
                {
                    ApplyToolAt(activeLayerId, cx, cy);
                    _hoverDirty = true;
                }

                EditorUtility.SetDirty(asset);
                Repaint();
                lastMousePos = e.mousePosition;
            }

            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDraggingPaint = false;
            e.Use();
        }
    }


    private bool MetaSourceIsValid()
    {
        return _metaSource.x >= 0 && _metaSource.y >= 0 && _metaSource.z > 0;
    }

    private void ApplyToolAt(int layerId, int x, int y)
    {
        switch (tool)
        {
            case ToolMode.Brush:
                asset.SetTileByLayerId(layerId, x, y, paintTileId);
                break;
            case ToolMode.Erase:
                asset.SetTileByLayerId(layerId, x, y, 0);
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

    private static string NormMeta(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();
    }

    private void FloodFillMetadata(int layerId, int sx, int sy, string targetRaw, string newRaw)
    {
        if ((uint)sx >= (uint)asset.width || (uint)sy >= (uint)asset.height) return;
        string normTarget = NormMeta(targetRaw);
        string normNew = NormMeta(newRaw);
        if (normTarget == normNew) return;

        var q = new Queue<Vector2Int>(256); q.Enqueue(new Vector2Int(sx, sy));
        while (q.Count > 0)
        {
            var p = q.Dequeue(); int x = p.x, y = p.y;
            if ((uint)x >= (uint)asset.width || (uint)y >= (uint)asset.height) continue;

            string here = asset.GetMetadata(layerId, x, y);
            if (NormMeta(here) != normTarget) continue;

            asset.SetMetadata(layerId, x, y, newRaw);
            q.Enqueue(new Vector2Int(x + 1, y)); q.Enqueue(new Vector2Int(x - 1, y)); q.Enqueue(new Vector2Int(x, y + 1)); q.Enqueue(new Vector2Int(x, y - 1));
        }
    }

    private void HandleShortcuts()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        bool typing = EditorGUIUtility.editingTextField || GUI.GetNameOfFocusedControl() == MetaTextControlName;
        if (typing) return;

        switch (e.keyCode)
        {
            case KeyCode.B:
                tool = ToolMode.Brush;
                Repaint();
                break;
            case KeyCode.E:
                tool = ToolMode.Erase;
                Repaint();
                break;
            case KeyCode.G:
                tool = ToolMode.Fill;
                Repaint();
                break;
            case KeyCode.I:
                tool = ToolMode.Picker;
                Repaint();
                break;
            case KeyCode.M:
                {
                    bool newMetaMode = !_metaMode;
                    if (newMetaMode != _metaMode)
                    {
                        _metaMode = newMetaMode;
                        if (!_metaMode)
                        {
                            // Turn off selection visually + clear panel selection
                            _metaTarget = new Vector3Int(-1, -1, -1);
                            _metaSource = new Vector3Int(-1, -1, -1);
                            _metaText = string.Empty;
                            GUI.FocusControl(null);
                        }
                        Repaint();
                    }
                    break;
                }
            case KeyCode.Alpha1:
            case KeyCode.Keypad1:
                SetLayerByHotkeyIndex(0); // 1st (lowest id)
                break;
            case KeyCode.Alpha2:
            case KeyCode.Keypad2:
                SetLayerByHotkeyIndex(1);
                break;
            case KeyCode.Alpha3:
            case KeyCode.Keypad3:
                SetLayerByHotkeyIndex(2);
                break;
            case KeyCode.Alpha4:
            case KeyCode.Keypad4:
                SetLayerByHotkeyIndex(3);
                break;
            case KeyCode.Alpha5:
            case KeyCode.Keypad5:
                SetLayerByHotkeyIndex(4);
                break;
            case KeyCode.Alpha6:
            case KeyCode.Keypad6:
                SetLayerByHotkeyIndex(5);
                break;
            case KeyCode.Alpha7:
            case KeyCode.Keypad7:
                SetLayerByHotkeyIndex(6);
                break;
            case KeyCode.Alpha8:
            case KeyCode.Keypad8:
                SetLayerByHotkeyIndex(7);
                break;
            case KeyCode.Alpha9:
            case KeyCode.Keypad9:
                SetLayerByHotkeyIndex(8);
                break;
            case KeyCode.Alpha0:
            case KeyCode.Keypad0:
                SetLayerByHotkeyIndex(9); // 10th lowest id
                break;
        }
    }

    private void SetLayerByHotkeyIndex(int index)
    {
        if (currentLayerIds == null) return;
        if (index < 0 || index >= currentLayerIds.Length) return;

        activeLayerIndex = index;
        Repaint();
    }

    private (int x, int y, bool onGrid) MouseCell(Vector2 mouse, Vector2 origin, float cell)
    {
        int x = Mathf.FloorToInt((mouse.x - origin.x) / cell);
        int y = Mathf.FloorToInt((mouse.y - origin.y) / cell);
        bool on = x >= 0 && x < asset.width && y >= 0 && y < asset.height;
        return (x, y, on);
    }

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
    {
        var cc = c;
        if (cc.a <= 0f) cc = new Color(0.6f, 0.6f, 0.6f, 1f);
        cc.a = a;
        EditorGUI.DrawRect(r, cc);
    }

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
            for (int i = 0; i < L.tiles.Length; i++)
            {
                int id = L.tiles[i];
                if (id <= 0) continue; // keep 0/empty
                if (!tileIdToLayerId.TryGetValue(id, out var reqLayer) || reqLayer != layerId)
                    L.tiles[i] = -1;
            }
        }
    }

    private void BeginMetaTypingUndoIfNeeded()
    {
        if (_metaUndoArmed || asset == null) return;

        _metaUndoArmed = true;
        Undo.IncrementCurrentGroup();
        _metaUndoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Edit Metadata");
        Undo.RecordObject(asset, "Edit Metadata");
    }

    private void CommitMetaIfDirty(bool force)
    {
        if (!_metaDirty || asset == null) return;
        if (_metaTarget.x < 0 || _metaTarget.y < 0 || _metaTarget.z <= 0) { _metaDirty = false; return; }

        double now = EditorApplication.timeSinceStartup;
        if (!force && (now - _metaLastEditTime) < MetaCommitDelay) return;

        // Commit: write-through to asset
        string val = string.IsNullOrWhiteSpace(_metaText) ? null : _metaText;
        asset.SetMetadata(_metaTarget.z, _metaTarget.x, _metaTarget.y, val);

        EditorUtility.SetDirty(asset);
        _hoverDirty = true;
        _metaDirty = false;

        // Close the undo group once we commit
        if (_metaUndoArmed && _metaUndoGroup >= 0)
        {
            Undo.CollapseUndoOperations(_metaUndoGroup);
            _metaUndoArmed = false;
            _metaUndoGroup = -1;
        }
    }

    private void CancelMetaTypingSession()
    {
        _metaDirty = false;
        _metaUndoArmed = false;
        _metaUndoGroup = -1;
        _metaLastEditTime = -1;
    }

}
#endif
