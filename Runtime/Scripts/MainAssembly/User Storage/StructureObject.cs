// File: StructureObject.cs
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.UserData
{
    /// <summary>
    /// A paintable, multi-layer tile structure asset. Stores tile IDs per layer in a compact 1D array.
    /// - Origin at bottom-left (x increases right, y increases up)
    /// - Tile ID of 0 means "empty" / no tile
    /// - Tile ID of -1 is reserved for "mask/inherit" (special editor meaning)
    /// - Layers are ordered; earlier indices render below later ones
    /// </summary>
    [CreateAssetMenu(fileName = "NewStructure", menuName = "RandomGenerator/UserData/Structure", order = 1000)]
    public class StructureObject : ScriptableObject
    {
        [Min(1)] public int width = 16;
        [Min(1)] public int height = 16;

        // Registry snapshots for validation (do not edit by hand)
        [SerializeField] private int[] expectedLayerIds = Array.Empty<int>();
        [SerializeField] private int[] expectedPaletteIds = Array.Empty<int>();

        [Serializable]
        public class LayerData
        {
            public int layerId;                // matches TileLayer.id
            public bool visible = true;        // editor-only convenience
            public bool locked = false;        // editor-only convenience
            [HideInInspector] public int[] tiles; // len = width*height; 0 == empty
        }

        [SerializeField] private List<LayerData> layers = new();

        // NEW: per-cell metadata, keyed by (layerId, x, y)
        [Serializable]
        public struct UnparsedMetadataEntry
        {
            public int layerId;
            public int x;
            public int y;
            [TextArea]
            public string raw; // e.g. "field:1, other:2"
        }

        [SerializeField] private List<UnparsedMetadataEntry> metadata = new(); // NEW

        public IReadOnlyList<LayerData> Layers => layers;
        public IReadOnlyList<int> ExpectedLayerIds => expectedLayerIds;
        public IReadOnlyList<int> ExpectedPaletteIds => expectedPaletteIds;

        // NEW: read-only view of metadata list (for later parsing / tools if needed)
        public IReadOnlyList<UnparsedMetadataEntry> Metadata => metadata;

        private int Idx(int x, int y) => y * width + x;
        private int2 IndexToPos(int index) => new int2(index % width, index / width);

        private int PositionToTileGridIndex(int x, int y, int z) => TileLayerRegistry.LayerCount * (y * width + x) + z;

        // NEW: get raw metadata for (layerId, x, y)
        public string GetMetadata(int layerId, int x, int y)
        {
            if (metadata == null) return null;
            for (int i = 0; i < metadata.Count; i++)
            {
                var m = metadata[i];
                if (m.layerId == layerId && m.x == x && m.y == y)
                    return m.raw;
            }
            return null;
        }

        /// <summary>
        /// Set raw metadata for a specific (layerId, x, y). Passing null/empty removes the entry.
        /// </summary>
        public void SetMetadata(int layerId, int x, int y, string raw)
        {
            if (metadata == null) metadata = new List<UnparsedMetadataEntry>();

            int idx = metadata.FindIndex(m => m.layerId == layerId && m.x == x && m.y == y);

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (idx >= 0) metadata.RemoveAt(idx);
                return;
            }

            var entry = new UnparsedMetadataEntry
            {
                layerId = layerId,
                x = x,
                y = y,
                raw = raw.Trim()
            };

            if (idx >= 0) metadata[idx] = entry;
            else metadata.Add(entry);
        }

        public MetadataEntry[] ConvertMetadata()
        {
            if (metadata == null || metadata.Count == 0)
                return Array.Empty<MetadataEntry>();

            var result = new List<MetadataEntry>(metadata.Count * 2); // rough guess

            foreach (var m in metadata)
            {
                if (string.IsNullOrWhiteSpace(m.raw))
                    continue;

                // Flip Y to match runtime / TileGrid convention
                int flippedY = height - 1 - m.y;

                // Example raw: "field:1, other:2"
                var segments = m.raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                {
                    var trimmed = seg.Trim();
                    if (trimmed.Length == 0)
                        continue;

                    int colonIndex = trimmed.IndexOf(':');
                    if (colonIndex <= 0 || colonIndex >= trimmed.Length - 1)
                        continue; // no proper "field:value"

                    string keyStr = trimmed.Substring(0, colonIndex).Trim();
                    string valStr = trimmed.Substring(colonIndex + 1).Trim();

                    if (string.IsNullOrEmpty(keyStr))
                        continue;

                    if (!int.TryParse(valStr, out int value))
                        continue; // ignore non-int values for now

                    // Optional: truncate overly long keys to avoid FixedString overflow
                    if (keyStr.Length > 64)
                        keyStr = keyStr.Substring(0, 64);

                    var entry = new MetadataEntry
                    {
                        layerId = m.layerId,
                        x = m.x,
                        y = flippedY,
                        field = new FixedString64Bytes(keyStr),
                        value = value
                    };

                    result.Add(entry);
                }
            }

            return result.ToArray();
        }

        public void InitializeIfEmpty(int[] layerIds, int[] paletteIds)
        {
            if (layers.Count == 0)
            {
                expectedLayerIds = (int[])layerIds.Clone();
                expectedPaletteIds = (int[])paletteIds.Clone();
                foreach (var lid in layerIds)
                    layers.Add(new LayerData { layerId = lid, tiles = NewBlank() }); // 0-filled
            }
            else
            {
                EnsureTileArrays();
            }
        }

        public void UpdateSnapshots(int[] layerIds, int[] paletteIds)
        {
            expectedLayerIds = (int[])layerIds.Clone();
            expectedPaletteIds = (int[])paletteIds.Clone();
        }

        public void EnsureTileArrays()
        {
            int len = Mathf.Max(1, width * height);
            foreach (var L in layers)
            {
                if (L.tiles == null || L.tiles.Length != len)
                {
                    var na = NewBlank(); // 0-filled
                    if (L.tiles != null)
                    {
                        int min = Mathf.Min(len, L.tiles.Length);
                        Array.Copy(L.tiles, na, min);
                    }
                    L.tiles = na;
                }
            }
        }

        private int[] NewBlank()
        {
            var a = new int[width * height];
            // default(int) = 0, so no loop needed
            return a;
        }

        public int GetTileByLayerId(int layerId, int x, int y)
        {
            var L = layers.Find(l => l.layerId == layerId);
            if (L == null || L.tiles == null) return 0;
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) return 0;
            return L.tiles[Idx(x, y)];
        }

        public void SetTileByLayerId(int layerId, int x, int y, int tileId)
        {
            var L = layers.Find(l => l.layerId == layerId);
            if (L == null) return;
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) return;
            if (L.tiles == null || L.tiles.Length != width * height) EnsureTileArrays();
            L.tiles[Idx(x, y)] = tileId;
        }

        public void Resize(int newW, int newH, Vector2Int anchor)
        {
            newW = Mathf.Max(1, newW);
            newH = Mathf.Max(1, newH);

            int oldW = width;
            int oldH = height;

            foreach (var L in layers)
            {
                var na = new int[newW * newH]; // 0-filled
                if (L.tiles != null)
                {
                    int copyW = Mathf.Min(oldW, newW);
                    int copyH = Mathf.Min(oldH, newH);
                    for (int y = 0; y < copyH; y++)
                        for (int x = 0; x < copyW; x++)
                        {
                            int dx = x + anchor.x, dy = y + anchor.y;
                            if ((uint)dx >= (uint)newW || (uint)dy >= (uint)newH) continue;
                            na[dy * newW + dx] = L.tiles[y * oldW + x];
                        }
                }
                L.tiles = na;
            }

            // NEW: remap metadata using same anchor logic
            if (metadata != null && metadata.Count > 0)
            {
                var newList = new List<UnparsedMetadataEntry>(metadata.Count);
                foreach (var m in metadata)
                {
                    int nx = m.x + anchor.x;
                    int ny = m.y + anchor.y;
                    if ((uint)nx >= (uint)newW || (uint)ny >= (uint)newH)
                        continue;

                    newList.Add(new UnparsedMetadataEntry
                    {
                        layerId = m.layerId,
                        x = nx,
                        y = ny,
                        raw = m.raw
                    });
                }
                metadata = newList;
            }

            width = newW;
            height = newH;
        }

        public void RebindLayersTo(int[] layerIds)
        {
            var byId = new Dictionary<int, LayerData>(layers.Count);
            foreach (var L in layers) byId[L.layerId] = L;
            var rebuilt = new List<LayerData>(layerIds.Length);
            foreach (var lid in layerIds)
            {
                if (byId.TryGetValue(lid, out var L))
                {
                    if (L.tiles == null || L.tiles.Length != width * height) L.tiles = NewBlank(); // 0-filled
                    rebuilt.Add(L);
                }
                else
                {
                    rebuilt.Add(new LayerData { layerId = lid, tiles = NewBlank() }); // 0-filled
                }
            }
            layers = rebuilt;
            expectedLayerIds = (int[])layerIds.Clone();
        }

        public void ClearLayerById(int layerId)
        {
            var L = layers.Find(l => l.layerId == layerId);
            if (L?.tiles == null) return;
            Array.Fill(L.tiles, 0);

            // NEW: also clear metadata on that layer
            if (metadata != null && metadata.Count > 0)
            {
                metadata.RemoveAll(m => m.layerId == layerId);
            }
        }

        /// <summary>Sets ALL tiles in ALL layers to the given value (usually 0).</summary>
        public void SetAllTiles(int value)
        {
            foreach (var L in layers)
            {
                if (L.tiles == null) continue;
                Array.Fill(L.tiles, value);
            }

            // NEW: clearing all tiles -> nuke metadata as well
            if (value == 0 && metadata != null)
                metadata.Clear();
        }

        // --- Border growth/shrink utilities ---
        public void AddColumnsLeft(int n) { if (n <= 0) return; GrowShrink(n, 0, 0, 0); }
        public void AddColumnsRight(int n) { if (n <= 0) return; GrowShrink(0, n, 0, 0); }
        public void AddRowsTop(int n) { if (n <= 0) return; GrowShrink(0, 0, 0, n); }
        public void AddRowsBottom(int n) { if (n <= 0) return; GrowShrink(0, 0, n, 0); }

        public void RemoveColumnsLeft(int n) { if (n <= 0 || n >= width) return; Trim(n, 0, 0, 0); }
        public void RemoveColumnsRight(int n) { if (n <= 0 || n >= width) return; Trim(0, n, 0, 0); }
        public void RemoveRowsTop(int n) { if (n <= 0 || n >= height) return; Trim(0, 0, 0, n); }
        public void RemoveRowsBottom(int n) { if (n <= 0 || n >= height) return; Trim(0, 0, n, 0); }

        private void GrowShrink(int left, int right, int down, int up)
        {
            int oldW = width;
            int oldH = height;

            int newW = Mathf.Max(1, width + left + right);
            int newH = Mathf.Max(1, height + down + up);
            foreach (var L in layers)
            {
                var na = new int[newW * newH]; // 0-filled
                for (int y = 0; y < oldH; y++)
                    for (int x = 0; x < oldW; x++)
                    {
                        int nx = x + left;
                        int ny = y + down;
                        if ((uint)nx >= (uint)newW || (uint)ny >= (uint)newH) continue;
                        na[ny * newW + nx] = L.tiles[Idx(x, y)];
                    }
                L.tiles = na;
            }

            // NEW: remap metadata by same offset
            if (metadata != null && metadata.Count > 0)
            {
                var newList = new List<UnparsedMetadataEntry>(metadata.Count);
                foreach (var m in metadata)
                {
                    int nx = m.x + left;
                    int ny = m.y + down;
                    if ((uint)nx >= (uint)newW || (uint)ny >= (uint)newH) continue;

                    newList.Add(new UnparsedMetadataEntry
                    {
                        layerId = m.layerId,
                        x = nx,
                        y = ny,
                        raw = m.raw
                    });
                }
                metadata = newList;
            }

            width = newW;
            height = newH;
        }

        private void Trim(int left, int right, int down, int up)
        {
            int oldW = width;
            int oldH = height;

            int newW = Mathf.Max(1, width - left - right);
            int newH = Mathf.Max(1, height - down - up);
            foreach (var L in layers)
            {
                var na = new int[newW * newH]; // 0-filled
                for (int y = 0; y < newH; y++)
                    for (int x = 0; x < newW; x++)
                    {
                        na[y * newW + x] = L.tiles[(y + down) * oldW + (x + left)];
                    }
                L.tiles = na;
            }

            // NEW: remap metadata; shift positions and cull out-of-bounds
            if (metadata != null && metadata.Count > 0)
            {
                var newList = new List<UnparsedMetadataEntry>(metadata.Count);
                foreach (var m in metadata)
                {
                    int nx = m.x - left;
                    int ny = m.y - down;
                    if ((uint)nx >= (uint)newW || (uint)ny >= (uint)newH) continue;

                    newList.Add(new UnparsedMetadataEntry
                    {
                        layerId = m.layerId,
                        x = nx,
                        y = ny,
                        raw = m.raw
                    });
                }
                metadata = newList;
            }

            width = newW;
            height = newH;
        }

        public void CollapseToBounds()
        {
            int minX = width, minY = height, maxX = -1, maxY = -1;
            foreach (var L in layers)
            {
                if (L.tiles == null) continue;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        if (L.tiles[Idx(x, y)] != 0)
                        {
                            minX = Math.Min(minX, x);
                            minY = Math.Min(minY, y);
                            maxX = Math.Max(maxX, x);
                            maxY = Math.Max(maxY, y);
                        }
                    }
            }
            if (maxX < minX || maxY < minY)
            {
                Resize(1, 1, Vector2Int.zero);
                foreach (var L in layers) if (L.tiles != null) Array.Fill(L.tiles, 0);

                // NEW: no tiles => clear metadata
                metadata?.Clear();

                return;
            }

            // Trim will also remap metadata due to our changes above
            Trim(minX, width - maxX - 1, minY, height - maxY - 1);
        }

        public int[] ConvertToIntArray()
        {
            int depth = TileLayerRegistry.LayerCount;
            int lenXY = width * height;
            var array = new int[lenXY * depth];

            foreach (LayerData data in layers)
            {
                int z = TileLayerRegistry.GetLayerZ(data.layerId);
                if (z < 0) continue;

                int index = 0;
                int max = Math.Min(data.tiles.Length, lenXY);

                while (index < max)
                {
                    int2 pos = IndexToPos(index);     // pos.y is editor-space (0 = bottom)

                    // Flip Y for runtime structure:
                    // editor: 0 = bottom, height-1 = top
                    // runtime structure: 0 = top, height-1 = bottom (or vice-versa)
                    int flippedY = height - 1 - pos.y;

                    int arrayIndex = PositionToTileGridIndex(pos.x, flippedY, z);
                    array[arrayIndex] = data.tiles[index];

                    index++;
                }
            }

            return array;
        }
    }
}
