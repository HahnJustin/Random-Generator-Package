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
        [SerializeField] private Vector2 gameObjectOffset = new Vector2(0.5f,0.5f);
        [SerializeField] private Transform gameObjectParent;
        private List<GameObject> spawnedObjects = new();

        [Header("Number Tiles")]
        [SerializeField] private bool makeNumberLayer = false;

        private GenerationManager randomGenerator;

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
            TileType type = tile.GetTypeInLayer(layer);

            GameObject prefab = randomGenerator.GetGameObject(type);
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
                    TileBase tileBase = randomGenerator.GetTileBase(tile.GetTypeInLayer(layer));
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

        public IEnumerator SetTilesCoroutine(LayerType layer, int tilesPerFrame = 500, float delay = 0f, int seed = 0)
        {
            if (layer == LayerType.NA) yield break;

            Tilemap tilemap = tilemapDict[layer];
            tilemap.ClearAllTiles();

            int width = tileGrid.width;
            int height = tileGrid.height;
            Vector2 center = new Vector2(width / 2f, height / 2f);

            System.Random rand = new(seed);
            int blockCountX = Mathf.CeilToInt((float)width / blockSize);
            int blockCountY = Mathf.CeilToInt((float)height / blockSize);

            // Step 1: Create blocks with noise-weighted bias
            List<(int startX, int startY, float sortKey)> blocks = new();

            for (int by = 0; by < blockCountY; by++)
            {
                for (int bx = 0; bx < blockCountX; bx++)
                {
                    float blockCenterX = (bx + 0.5f) * blockSize;
                    float blockCenterY = (by + 0.5f) * blockSize;
                    float dist = Vector2.Distance(new Vector2(blockCenterX, blockCenterY), center);
                    float bias = 1f / (dist + 1f);
                    float noise = (float)rand.NextDouble();
                    float sortKey = noise + (1f - bias) * 0.5f; // Center preference + randomness
                    blocks.Add((bx * blockSize, by * blockSize, sortKey));
                }
            }

            // Step 2: Sort blocks based on noise+bias
            blocks = blocks.OrderBy(b => b.sortKey).ToList();

            // Step 3: Load each block
            foreach (var (startX, startY, _) in blocks)
            {
                for (int y = 0; y < blockSize; y++)
                {
                    for (int x = 0; x < blockSize; x++)
                    {
                        int tx = startX + x;
                        int ty = startY + y;
                        if (tx >= width || ty >= height) continue;

                        int tempIndex = tx + ty * width;
                        Core.Tile tile = tileGrid.GetTile(tx, ty);
                        TileBase tileBase = randomGenerator.GetTileBase(tile.GetTypeInLayer(layer));

                        if (useGameObjects && SpawnTileGameObject(tile, layer))
                        {
                            tilemap.SetTile(new Vector3Int(tx, ty, 0), null);
                        }
                        else
                        {
                            tilemap.SetTile(new Vector3Int(tx, ty, 0), tileBase);
                        }
                    }
                }

                yield return new WaitForSeconds(delay);
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
                    TileBase tileBase = randomGenerator.GetNumberTileBase(tile.Value);
                    tileBaseArray[tempIndex] = tileBase;
                }
            }

            numberTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tileBaseArray);
        }

        private void CreateTileMap(LayerType layer)
        {
            if (layer == LayerType.NA) return;
            GameObject tilemapObject = Instantiate(TilemapPrefab, transform);
            int sortingOrder = randomGenerator.GetSortingOrder(layer);
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            tilemapObject.GetComponent<Renderer>().sortingLayerID = randomGenerator.GetSortingLayerID(layer);

            Material material = randomGenerator.GetMaterial(layer);
            if(material != null)tilemapObject.GetComponent<Renderer>().material = material;

            int number = randomGenerator.GetLayerID(layer);
            tilemapObject.layer = number;

            tilemapObject.tag = randomGenerator.GetTag(layer);

            if (randomGenerator.GetHasCollider(layer))
            {
                TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
                if (randomGenerator.GetUseCompositeCollider(layer))
                {
                    CompositeCollider2D compColl = tilemapObject.AddComponent<CompositeCollider2D>();

                    Rigidbody2D rb = tilemapObject.GetComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;
                    rb.simulated = true;

                    tilemapCollider.usedByComposite = true;
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

            if (tileGrid == null || !tileGrid.IsDataValid)
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
                    StartCoroutine(SetTilesCoroutine(layer, tilesPerFrame: tilesPerFrame, delay: 0f, seed: seed));
            }

            if (makeNumberLayer && numberTilemap == null) CreateNumberTileMap();
            else if (!makeNumberLayer && numberTilemap != null) Destroy(numberTilemap.gameObject);

            if (makeNumberLayer) SetNumberTiles();
        }

        public void SetRandomGenerator(GenerationManager randomGenerator)
        {
            if (randomGenerator == null)
            {
                Debug.LogError("RandomGenerator is null");
                return;
            }

            this.randomGenerator = randomGenerator;
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