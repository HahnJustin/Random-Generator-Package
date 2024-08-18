using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dalichrome.RandomGenerator
{
    public class TilemapCreator : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private GameObject TilemapPrefab;
        [SerializeField] private List<SerialPair<LayerType, Tilemap>> tilemaps;
        [SerializeField] private bool instantiateMissingTilemaps = true;

        [Header("GameObjects")]
        [SerializeField] private bool useGameObjects = false;
        [SerializeField] private Transform gameObjectParent;

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

        private bool SpawnTileGameObject(Tile tile, LayerType layer)
        {
            TileType type = tile.GetTypeInLayer(layer);

            GameObject prefab = randomGenerator.GetGameObject(type);
            if (prefab == null) return false;

            Instantiate(prefab, (Vector3Int)tile.Vector, Quaternion.identity, gameObjectParent);
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
                    Tile tile = tileGrid.GetTile(x, y);
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
                    Tile tile = tileGrid.GetTile(x, y);
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
            if (randomGenerator.GetHasCollider(layer)) tilemapObject.AddComponent<TilemapCollider2D>();

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

        public void SetTileGrid(TileGrid tileGrid)
        {
            if (tilemapDict == null)
            {
                CreateDictionary();
            }

            if (tileGrid == null)
            {
                Debug.LogError("TileGrid is null");
                return;
            }
            this.tileGrid = tileGrid;

            foreach (LayerType layer in Enum.GetValues(typeof(LayerType)))
            {
                if (!tilemapDict.ContainsKey(layer) && instantiateMissingTilemaps)
                {
                    CreateTileMap(layer);
                }
                SetTilesByLayer(layer);
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
    }
}