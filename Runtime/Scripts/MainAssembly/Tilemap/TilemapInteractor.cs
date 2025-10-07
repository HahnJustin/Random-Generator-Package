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
    public class TilemapInteractor: MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] protected GameObject TilemapPrefab;

        [Header("GameObjects")]
        [SerializeField] protected bool useGameObjects = false;
        [SerializeField] protected float gameObjectVariance = 0.2f;
        [SerializeField] protected Vector2 gameObjectOffset = new (0.5f,0.5f);
        [SerializeField] protected Transform gameObjectParent;
        protected List<GameObject> spawnedObjects = new();

        [Header("Number Tiles")]
        [SerializeField] protected bool makeNumberLayer = false;

        protected RandomGenerator randomGenerator;

        protected Dictionary<LayerType, Tilemap> tilemapDict = new();
        protected Tilemap numberTilemap;

        protected TileGrid tileGrid;

        protected bool SpawnTileGameObject(Core.Tile tile, LayerType layer)
        {
            int tileId = tile.GetIdInLayer(layer);

            GameObject prefab = TileObjectInfo.GetGameObject(tileId);
            if (prefab == null) return false;

            Vector2 circle = UnityEngine.Random.insideUnitCircle * gameObjectVariance;

            GameObject spawned = Instantiate(prefab, new Vector3(tile.Position.x + circle.x + gameObjectOffset.x,
                                            tile.Position.y + circle.y + gameObjectOffset.y,
                                            prefab.transform.position.z), Quaternion.identity, gameObjectParent);
            spawnedObjects.Add(spawned);
            return true;
        }

        protected void SetTilesByLayer(LayerType layer)
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
                    TileBase tileBase = TileObjectInfo.GetTileBase(tile.GetIdInLayer(layer));
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
                    Core.Tile tile = tileGrid.GetTile(x, y);
                    TileBase tileBase = TileObjectInfo.GetNumberTileBase(tile.Value);
                    tileBaseArray[tempIndex] = tileBase;
                }
            }

            numberTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tileBaseArray);
        }

        protected void CreateTileMap(LayerType layer)
        {
            if (layer == LayerType.NA) return;
            GameObject tilemapObject = Instantiate(TilemapPrefab, transform);
            int sortingOrder = TileLayerInfo.GetSortingOrder((int)layer);
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            tilemapObject.GetComponent<Renderer>().sortingLayerID = TileLayerInfo.GetSortingLayerID((int)layer);

            Material material = TileLayerInfo.GetMaterial((int)layer);
            if(material != null)tilemapObject.GetComponent<Renderer>().material = material;

            int number = TileLayerInfo.GetUnityLayerID((int)layer);
            tilemapObject.layer = number;

            tilemapObject.tag = TileLayerInfo.GetTag((int)layer);

            if (TileLayerInfo.GetHasCollider((int)layer))
            {
                TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
                if (TileLayerInfo.GetUseCompositeCollider((int)layer))
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

        protected void CreateNumberTileMap()
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

        public virtual void CreateWithTilegrid(TileGrid tileGrid)
        {
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
                if (!tilemapDict.ContainsKey(layer))
                {
                    CreateTileMap(layer);
                }

                SetTilesByLayer(layer);
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