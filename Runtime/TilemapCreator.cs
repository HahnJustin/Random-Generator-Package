using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Random;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator
{
    public class TilemapCreator : TilemapInteractor
    {
        [Header("Creator")]
        [SerializeField] private List<SerialPair<LayerType, Tilemap>> tilemaps;
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
            foreach (SerialPair<LayerType, Tilemap> pair in tilemaps)
            {
                tilemapDict[pair.Key] = pair.Value;
            }
        }

        public IEnumerator SetTilesCoroutine(
                LayerType layer,
                int tilesPerFrame = 2_000,   // how many tiles youÅfre OK pushing in one frame
                int seed = 0)
        {
            if (layer == LayerType.NA) yield break;

            Tilemap tilemap = tilemapDict[layer];
            tilemap.ClearAllTiles();

            int width = tileGrid.width;
            int height = tileGrid.height;
            int blocksX = Mathf.CeilToInt((float)width / blockSize);
            int blocksY = Mathf.CeilToInt((float)height / blockSize);
            Vector2 centre = new (width * 0.5f, height * 0.5f);

            // ---------- 1.  build + sort block list  ----------
            var rng = new System.Random(seed);
            var blocks = new List<(int sx, int sy, float key)>(blocksX * blocksY);

            for (int by = 0; by < blocksY; ++by)
            {
                for (int bx = 0; bx < blocksX; ++bx)
                {
                    float cx = (bx + 0.5f) * blockSize;
                    float cy = (by + 0.5f) * blockSize;
                    float dist = Vector2.Distance(new (cx, cy), centre);
                    float bias = 1f / (dist + 1f);              // centre-weighted
                    float key = (float) rng.NextDouble() + (1f - bias) * .5f;

                    blocks.Add((bx * blockSize, by * blockSize, key));
                }
            }
            blocks.Sort((a, b) => a.key.CompareTo(b.key));
            // -----------------------------------------------

            // reusable buffer (no per-frame GC allocs)
            TileBase[] buf = new TileBase[blockSize * blockSize];

            int tilesDoneThisFrame = 0;

            // ---------- 2.  stream blocks ----------
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
                        {   // outside map Å® clear
                            buf[bufIdx] = null;
                            continue;
                        }

                        ITileColumn col = tileGrid.GetColumn(tx, ty);
                        if (useGameObjects && SpawnTileGameObject(col, layer))
                            buf[bufIdx] = null;
                        else
                            buf[bufIdx] =
                                TileObjectInfo.GetTileBase(col[TileLayerInfo.GetLayerZ((int)layer)]);
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

        private void SetNumberTiles()
        {
            numberTilemap.ClearAllTiles();

            int width = tileGrid.width;
            int height = tileGrid.height;

            TileBase[] tileBaseArray = new TileBase[width * height];

            foreach (int2 pos in tileGrid.GetPositions()) 
            {
                int tempIndex = pos.x + (pos.y * tileGrid.width);
                TileBase tileBase = TileObjectInfo.GetNumberTileBase(tileGrid.GetTileValue(pos));
                tileBaseArray[tempIndex] = tileBase;
            }

            numberTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tileBaseArray);
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

            int seed = UnityEngine.Random.Range(0,1000000);
            StopAllCoroutines();
            foreach (LayerType layer in Enum.GetValues(typeof(LayerType)))
            {
                if (!tilemapDict.ContainsKey(layer) && instantiateMissingTilemaps)
                {
                    CreateTileMap(layer);
                }

                if(!coroutineLoading)
                    SetTilesByLayer(layer);
                else
                    StartCoroutine(SetTilesCoroutine(layer, tilesPerFrame: tilesPerFrame, seed: seed));
            }

            if (makeNumberLayer && numberTilemap == null) CreateNumberTileMap();
            else if (!makeNumberLayer && numberTilemap != null) Destroy(numberTilemap.gameObject);

            if (makeNumberLayer) SetNumberTiles();
        }
    }
}