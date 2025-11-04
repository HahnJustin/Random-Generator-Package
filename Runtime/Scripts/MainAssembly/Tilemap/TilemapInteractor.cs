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

        protected Dictionary<int, Tilemap> tilemapDict = new();
        protected Tilemap numberTilemap;

        protected TileGrid tileGrid;

        protected bool SpawnTileGameObject(ITileColumn col, int layerId)
        {
            int tileId = col[TileLayerRegistry.GetLayerZ(layerId)];

            GameObject prefab = TileObjectRegistry.GetGameObject(tileId);
            if (prefab == null) return false;

            Vector2 circle = UnityEngine.Random.insideUnitCircle * gameObjectVariance;

            GameObject spawned = Instantiate(prefab, new Vector3(col.X + circle.x + gameObjectOffset.x,
                                                                 col.Y + circle.y + gameObjectOffset.y,
                                            prefab.transform.position.z), Quaternion.identity, gameObjectParent);
            spawnedObjects.Add(spawned);
            return true;
        }

        protected void SetTilesByLayer(int layerId)
        {
            if (layerId == 0) return;
            Tilemap tilemap = tilemapDict[layerId];
            tilemap.ClearAllTiles();

            int width = tileGrid.width;
            int height = tileGrid.height;

            TileBase[] tileBaseArray = new TileBase[width * height];

            foreach (ITileColumn col in tileGrid) 
            {
                int tempIndex = col.X + (col.Y * tileGrid.width);
                if (useGameObjects && SpawnTileGameObject(col, layerId)) {
                    tileBaseArray[tempIndex] = null;
                }
                else
                {
                    TileBase tileBase = TileObjectRegistry.GetTileBase(col[TileLayerRegistry.GetLayerZ(layerId)]);
                    tileBaseArray[tempIndex] = tileBase;
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

            foreach (int2 pos in tileGrid.GetPositions())
            {
                int tempIndex = pos.x + (pos.y * tileGrid.width);
                    TileBase tileBase = TileObjectRegistry.GetNumberTileBase(tileGrid.GetTileValue(pos));
                    tileBaseArray[tempIndex] = tileBase;
                
            }

            numberTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), tileBaseArray);
        }

        protected void CreateTileMap(int layerId)
        {
            if (layerId == 0) return;
            GameObject tilemapObject = Instantiate(TilemapPrefab, transform);
            int sortingOrder = TileLayerRegistry.GetSortingOrder(layerId);
            tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            tilemapObject.GetComponent<Renderer>().sortingLayerID = TileLayerRegistry.GetSortingLayerID(layerId);

            Material material = TileLayerRegistry.GetMaterial(layerId);
            if(material != null)tilemapObject.GetComponent<Renderer>().material = material;

            tilemapObject.layer = TileLayerRegistry.GetUnityLayerID(layerId);

            tilemapObject.tag = TileLayerRegistry.GetTag(layerId);

            if (TileLayerRegistry.GetHasCollider(layerId))
            {
                TilemapCollider2D tilemapCollider = tilemapObject.AddComponent<TilemapCollider2D>();
                if (TileLayerRegistry.GetUseCompositeCollider(layerId))
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
            tilemapDict[layerId] = tilemap;
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
            foreach (int id in TileLayerRegistry.AllLayerIds)
            {
                if (!tilemapDict.ContainsKey(id))
                {
                    CreateTileMap(id);
                }

                SetTilesByLayer(id);
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

        public Dictionary<int,Tilemap> GetTilemapDictionary()
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