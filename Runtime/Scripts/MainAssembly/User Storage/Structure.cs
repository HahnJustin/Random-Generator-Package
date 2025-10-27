// File: StructureEditor.cs
// Place under an Editor assembly folder (e.g., /Editor). One file contains the asset, editor window, and custom inspector.
// Namespace aligned with your Dalichrome.RandomGenerator ecosystem.

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.UserData
{
    /// <summary>
    /// A paintable, multi-layer tile structure asset. Stores tile IDs per layer in a compact 1D array.
    /// - Origin at bottom-left (x increases right, y increases up)
    /// - Tile ID of -1 means "empty" / no tile
    /// - Layers are ordered; earlier indices render below later ones
    /// </summary>
    [CreateAssetMenu(fileName = "NewStructure", menuName = "RandomGenerator/UserData/Structure", order = 1000)]
    public class Structure : ScriptableObject
    {
        [Min(1)] public int width = 16;
        [Min(1)] public int height = 16;

        // Registry snapshots for validation (do not edit by hand)
        [SerializeField] private int[] expectedLayerIds = Array.Empty<int>();
        [SerializeField] private int[] expectedPaletteIds = Array.Empty<int>();

        [Serializable]
        public class LayerData
        {
            public int layerId;               // matches TileLayer.id
            public bool visible = true;       // editor-only convenience
            public bool locked = false;      // editor-only convenience
            [HideInInspector] public int[] tiles; // len = width*height; -1 = empty
        }

        [SerializeField] private List<LayerData> layers = new();

        public IReadOnlyList<LayerData> Layers => layers;
        public IReadOnlyList<int> ExpectedLayerIds => expectedLayerIds;
        public IReadOnlyList<int> ExpectedPaletteIds => expectedPaletteIds;

        private int Idx(int x, int y) => y * width + x;
        private int2 IndexToPos(int index) => new int2(index % width, index / width);

        private int PositionToTileGridIndex(int x, int y, int z) => TileLayerInfo.LayerCount * (y * width + x) + z;

        public void InitializeIfEmpty(int[] layerIds, int[] paletteIds)
        {
            if (layers.Count == 0)
            {
                expectedLayerIds = (int[])layerIds.Clone();
                expectedPaletteIds = (int[])paletteIds.Clone();
                foreach (var lid in layerIds)
                    layers.Add(new LayerData { layerId = lid, tiles = NewBlank() });
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
                    var na = NewBlank();
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
            for (int i = 0; i < a.Length; i++) a[i] = -1;
            return a;
        }

        public int GetTileByLayerId(int layerId, int x, int y)
        {
            var L = layers.Find(l => l.layerId == layerId);
            if (L == null || L.tiles == null) return -1;
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) return -1;
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
            newW = Mathf.Max(1, newW); newH = Mathf.Max(1, newH);
            foreach (var L in layers)
            {
                var na = new int[newW * newH];
                for (int i = 0; i < na.Length; i++) na[i] = -1;
                if (L.tiles != null)
                {
                    int copyW = Mathf.Min(width, newW);
                    int copyH = Mathf.Min(height, newH);
                    for (int y = 0; y < copyH; y++)
                        for (int x = 0; x < copyW; x++)
                        {
                            int dx = x + anchor.x, dy = y + anchor.y;
                            if ((uint)dx >= (uint)newW || (uint)dy >= (uint)newH) continue;
                            na[dy * newW + dx] = L.tiles[y * width + x];
                        }
                }
                L.tiles = na;
            }
            width = newW; height = newH;
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
                    if (L.tiles == null || L.tiles.Length != width * height) L.tiles = NewBlank();
                    rebuilt.Add(L);
                }
                else
                {
                    rebuilt.Add(new LayerData { layerId = lid, tiles = NewBlank() });
                }
            }
            layers = rebuilt;
            expectedLayerIds = (int[])layerIds.Clone();
        }

        public void ClearLayerById(int layerId)
        {
            var L = layers.Find(l => l.layerId == layerId);
            if (L?.tiles == null) return;
            for (int i = 0; i < L.tiles.Length; i++) L.tiles[i] = -1;
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

        // Internal: grow with offsets (left/right add columns, down/up add rows)
        private void GrowShrink(int left, int right, int down, int up)
        {
            int newW = Mathf.Max(1, width + left + right);
            int newH = Mathf.Max(1, height + down + up);
            foreach (var L in layers)
            {
                var na = new int[newW * newH];
                for (int i = 0; i < na.Length; i++) na[i] = -1;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        int nx = x + left; // shift right by left
                        int ny = y + down; // shift up by down
                        if ((uint)nx >= (uint)newW || (uint)ny >= (uint)newH) continue;
                        na[ny * newW + nx] = L.tiles[Idx(x, y)];
                    }
                L.tiles = na;
            }
            width = newW; height = newH;
        }

        // Internal: trim from each border
        private void Trim(int left, int right, int down, int up)
        {
            int newW = Mathf.Max(1, width - left - right);
            int newH = Mathf.Max(1, height - down - up);
            foreach (var L in layers)
            {
                var na = new int[newW * newH];
                for (int i = 0; i < na.Length; i++) na[i] = -1;
                for (int y = 0; y < newH; y++)
                    for (int x = 0; x < newW; x++)
                    {
                        na[y * newW + x] = L.tiles[(y + down) * width + (x + left)];
                    }
                L.tiles = na;
            }
            width = newW; height = newH;
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
                        if (L.tiles[Idx(x, y)] >= 0)
                        { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
                    }
            }
            if (maxX < minX || maxY < minY)
            {
                Resize(1, 1, Vector2Int.zero);
                foreach (var L in layers) if (L.tiles != null) for (int i = 0; i < L.tiles.Length; i++) L.tiles[i] = -1;
                return;
            }
            Trim(minX, width - maxX - 1, minY, height - maxY - 1);
        }

        public int[] ConvertToIntArray()
        {
            int depth = TileLayerInfo.LayerCount;        // uses the registry's layer count/z-order
            int lenXY = width * height;
            var array = new int[lenXY * depth];

            foreach (LayerData data in layers)
            {
                int z = TileLayerInfo.GetLayerZ(data.layerId);
                if (z < 0) continue;
                int index = 0;
                int max = Math.Min(data.tiles.Length, lenXY);
                while (index < max)
                {
                    int2 pos = IndexToPos(index);
                    int arrayIndex = PositionToTileGridIndex(pos.x, pos.y, z);
                    array[arrayIndex] = data.tiles[index];
                    index++;
                }
            }

            return array;
        }
    }
}