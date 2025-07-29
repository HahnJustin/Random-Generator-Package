using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Random;
using System.ComponentModel;
using Unity.Mathematics;
using System.Linq;

namespace Dalichrome.RandomGenerator
{
    public class TilemapCreator : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private GameObject TilemapPrefab;
        [SerializeField] private List<SerialPair<LayerType, Tilemap>> tilemaps;
        [SerializeField] private bool instantiateMissingTilemaps = true;

        [Header("Coroutine Loading")]
        [SerializeField] private bool coroutineLoading = false;
        [SerializeField] private int tilesPerFrame = 1000;
        [SerializeField] private int blockSize = 3;

        [Header("GameObjects")]
        [SerializeField] private bool useGameObjects = false;
        [SerializeField] private float gameObjectVariance = 0.2f;
        [SerializeField] private Vector2 gameObjectOffset = new (0.5f,0.5f);
        [SerializeField] private Transform gameObjectParent;
        private List<GameObject> spawnedObjects = new();

        [Header("Number Tiles")]
        [SerializeField] private bool makeNumberLayer = false;

        private RandomGenerator randomGenerator;
        private TileInfoGrabber tileInfoGrabber;
        private LayerInfoGrabber layerInfoGrabber;

        private Dictionary<LayerType, Tilemap> tilemapDict;
        private Tilemap numberTilemap;

        private TileGrid tileGrid;

        private void Awake()
        {
            CreateDictionary();
        }

        private void CreateDictionary()
        {
            tilemapDict = new();
            foreach (SerialPair<LayerType, Tilemap> pair in tilemaps)
            {
                tilemapDict[pair.Key] = pair.Value;
            }
        }

        private bool SpawnTileGameObject(Core.Tile tile, LayerType layer)
        {
            int tileId = tile.GetIdInLayer(layer);

            GameObject prefab = tileInfoGrabber.GetGameObject(tileId);
            if (prefab == null) return false;

            Vector2 circle = UnityEngine.Random.insideUnitCircle * gameObjectVariance;

            GameObject spawned = Instantiate(prefab, new Vector3(tile.Position.x + circle.x + gameObjectOffset.x,
                                            tile.Position.y + circle.y + gameObjectOffset.y,
                                            prefab.transform.position.z), Quaternion.identity, gameObjectParent);
            spawnedObjects.Add(spawned);
            return true;
        }

        private void SetTilesByLayer(LayerType layer)
        {
            if (layer == LayerType.NA) return;
            Tilemap tilemap = tilemapDict[layer];
            tilemap.ClearAllTiles();

            int width = tileGrid.width;
            int height = tileGrid.height;

            TileBase[] tileBaseArray = new TileBase[width * height];

            for (int y = tileGrid.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < tileGrid.width; x++)
                {
                    int tempIndex = x + (y * tileGrid.width);
                    Core.Tile tile = tileGrid.GetTile(x, y);
                    TileBase tileBase = tileInfoGrabber.GetTileBase(tile.GetIdInLayer(layer));
                    if (useGameObjects && SpawnTileGameObject(tile, layer)) {
                        tileBaseArray[tempIndex] = null;
                    }
                    else
                    {
                        tileBaseArray[tempIndex] = tileBase;
                    }
                }
            }

            tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tileBaseArray);
        }

        public IEnumerator SetTilesCoroutine(
                LayerType layer,
                int tilesPerFrame = 2_000,   // how many tiles youfre OK pushing in one frame
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
                        {   // outside map ¨ clear
                            buf[bufIdx] = null;
                            continue;
                        }

                        var tile = tileGrid.GetTile(tx, ty);
                        if (useGameObjects && SpawnTileGameObject(tile, layer))
                            buf[bufIdx] = null;
                        else
                            buf[bufIdx] =
                                tileInfoGrabber.GetTileBase(tile.GetIdInLayer(layer));
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

            for (int y = tileGrid.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < tileGrid.width; x++)
                {
                    int tempIndex = x + (y * tileGrid.width);
                    Core.Tile tile = tileGrid.GetTile(x, y);
                    TileBase tileBase = tileInfoGrabber.GetNumberTileBase(tile.Value);
                    tileBaseArray[tempIndex] = tileBase;
                }
            }

            numberTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tileBaseArray);
        }

        private void CreateTileMap(LayerType layer)
        {
            if (layer == LayerType.NA) return;
            GameObject tilemapObject = Instantiate(TilemapPrefab, transform);
            int sortingOrder = layerInfoGrabber.GetSortingOrder(layer);
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            tilemapObject.GetComponent<Renderer>().sortingLayerID = layerInfoGrabber.GetSortingLayerID(layer);

            Material material = layerInfoGrabber.GetMaterial(layer);
            if(material != null)tilemapObject.GetComponent<Renderer>().material = material;

            int number = layerInfoGrabber.GetLayerID(layer);
            tilemapObject.layer = number;

            tilemapObject.tag = layerInfoGrabber.GetTag(layer);

            if (layerInfoGrabber.GetHasCollider(layer))
            {
                TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
                if (layerInfoGrabber.GetUseCompositeCollider(layer))
                {
                    CompositeCollider2D compColl = tilemapObject.AddComponent<CompositeCollider2D>();

                    Rigidbody2D rb = tilemapObject.GetComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;
                    rb.simulated = true;

                    tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Intersect;
                }
            }

            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogError("TilemapPrefab must have a Tilemap Component");
            }
            tilemapDict[layer] = tilemap;
        }

        private void CreateNumberTileMap()
        {
            GameObject tilemapObject = Instantiate(TilemapPrefab, transform);
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 10;
            Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogError("TilemapPrefab must have a Tilemap Component");
            }
            numberTilemap = tilemap;
            tilemapObject.SetActive(false);
        }

        public void CreateTilemaps(TileGrid tileGrid)
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

        public void SetRandomGenerator(RandomGenerator randomGenerator)
        {
            if (randomGenerator == null)
            {
                Debug.LogError("RandomGenerator is null");
                return;
            }

            this.randomGenerator = randomGenerator;
            tileInfoGrabber = randomGenerator.TileInfoGrabber;
            layerInfoGrabber = randomGenerator.LayerInfoGrabber;
        }

        public Dictionary<LayerType,Tilemap> GetTilemapDictionary()
        {
            return tilemapDict;
        }

        public Tilemap GetNumberTilemap()
        {
            return numberTilemap;
        }

        public List<GameObject> GetGameObjects()
        {
            return spawnedObjects;
        }
    }
}