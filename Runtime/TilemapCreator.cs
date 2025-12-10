using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dalichrome.RandomGenerator
{
    public class TilemapCreator : TilemapInteractor
    {
        [Header("Creator")]
        [SerializeField] private List<SerialPair<int, Tilemap>> tilemaps;
        [SerializeField] private bool instantiateMissingTilemaps = true;

        [Header("Coroutine Loading")]
        [SerializeField] private bool coroutineLoading = false;
        [SerializeField] private int tilesPerFrame = 1000;
        [SerializeField] private int blockSize = 3;

        private void Awake()
        {
            CreateDictionary();
        }

        private void CreateDictionary()
        {
            tilemapDict.Clear();
            foreach (SerialPair<int, Tilemap> pair in tilemaps)
            {
                tilemapDict[pair.Key] = pair.Value;
            }
        }

        public IEnumerator SetTilesCoroutine(
            int layerId,
            int tilesPerFrame = 2_000,
            int seed = 0)
        {
            if (layerId == 0) yield break;

            Tilemap tilemap = tilemapDict[layerId];
            tilemap.ClearAllTiles();

            int width = tileGrid.width;
            int height = tileGrid.height;
            int blocksX = Mathf.CeilToInt((float)width / blockSize);
            int blocksY = Mathf.CeilToInt((float)height / blockSize);
            Vector2 centre = new(width * 0.5f, height * 0.5f);

            // ---------- 1. build + sort block list ----------
            var rng = new System.Random(seed);
            var blocks = new List<(int sx, int sy, float key)>(blocksX * blocksY);

            for (int by = 0; by < blocksY; ++by)
            {
                for (int bx = 0; bx < blocksX; ++bx)
                {
                    float cx = (bx + 0.5f) * blockSize;
                    float cy = (by + 0.5f) * blockSize;
                    float dist = Vector2.Distance(new(cx, cy), centre);
                    float bias = 1f / (dist + 1f);  // centre-weighted
                    float key = (float)rng.NextDouble() + (1f - bias) * .5f;

                    blocks.Add((bx * blockSize, by * blockSize, key));
                }
            }
            blocks.Sort((a, b) => a.key.CompareTo(b.key));
            // -----------------------------------------------

            // reusable buffer (no per-frame GC allocs)
            TileBase[] buf = new TileBase[blockSize * blockSize];

            int tilesDoneThisFrame = 0;

            // ---------- 2. stream blocks ----------
            foreach (var (sx, sy, _) in blocks)
            {
                // fill buf ------------------------------------------------------------
                for (int y = 0; y < blockSize; ++y)
                {
                    int ty = sy + y;
                    bool tyOut = ty >= height;

                    for (int x = 0; x < blockSize; ++x)
                    {
                        int tx = sx + x;
                        int bufIdx = x + y * blockSize;

                        if (tyOut || tx >= width)
                        {
                            // outside map Å® clear
                            buf[bufIdx] = null;
                            continue;
                        }

                        ITileColumn col = tileGrid.GetColumn(tx, ty);
                        int2 pos = new int2(tx, ty);
                        int z = TileLayerRegistry.GetLayerZ(layerId);

                        int tileId = col[z];
                        List<MetaPair> metaPairs = null;

                        if (TileObjectRegistry.HasMetaVariants(tileId))
                        {
                            metaPairs = tileGrid.GetAllDataWithColData(new int3(pos.x, pos.y, z));
                            if (metaPairs == null || metaPairs.Count == 0)
                                metaPairs = null;
                        }

                        if (useGameObjects && SpawnTileGameObject(col, layerId, metaPairs))
                        {
                            buf[bufIdx] = null;
                        }
                        else
                        {
                            buf[bufIdx] = TileObjectRegistry.GetTileBase(tileId, metaPairs);
                        }
                    }
                }

                // push one bulk call --------------------------------------------------
                var bounds = new BoundsInt(sx, sy, 0, blockSize, blockSize, 1);
                tilemap.SetTilesBlock(bounds, buf);

                tilesDoneThisFrame += blockSize * blockSize;
                if (tilesDoneThisFrame >= tilesPerFrame)
                {
                    tilesDoneThisFrame = 0;
                    yield return new WaitForEndOfFrame();   // optional breather
                }
            }
        }

        public override void CreateWithTilegrid(TileGrid tileGrid)
        {
            if (tilemapDict == null)
            {
                CreateDictionary();
            }

            if (tileGrid == null || !tileGrid.IsValid)
            {
                Debug.LogError("TileGrid is not valid");
                return;
            }
            this.tileGrid = tileGrid;

            int seed = UnityEngine.Random.Range(0, 1000000);
            StopAllCoroutines();
            foreach (int layerId in TileLayerRegistry.AllLayerIds)
            {
                if (!tilemapDict.ContainsKey(layerId) && instantiateMissingTilemaps)
                {
                    CreateTileMap(layerId);
                }

                if (!coroutineLoading)
                    SetTilesByLayer(layerId);
                else
                    StartCoroutine(SetTilesCoroutine(layerId, tilesPerFrame: tilesPerFrame, seed: seed));
            }
        }
    }
}
