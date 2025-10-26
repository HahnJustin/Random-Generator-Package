using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator
{
    public class TilemapChunker: TilemapInteractor
    {
        [Header("Chunks")]
        [SerializeField] private int chunkSize = 16;
        [SerializeField] private int chunkDistance = 1;
        [SerializeField] private int chunkPerFrame = 2;
        [SerializeField] private bool debugging = false;

        private Chunk[] chunks;

        private GameObject toFollow;

        private int chunkWidth;
        private int chunkHeight;

        private Chunk mainChunk;
        private HashSet<Chunk> currentChunks = new();
        private readonly List<Chunk> scratch = new(64);
        private BoundsInt chunkBounds;

        private Dictionary<int, TileBase[]> chunkLayerBuffers = new();
        private TileBase[] emptyChunkBuffer;

        private readonly Queue<Chunk> renderQueue = new();
        private readonly Queue<Chunk> derenderQueue = new();
        private readonly HashSet<Chunk> inRenderQ = new();
        private readonly HashSet<Chunk> inDerenderQ = new();

        private bool WithinDistanceOfMainChunk(Chunk a) =>
            Mathf.Abs(a.chunkX - mainChunk.chunkX) <= chunkDistance &&
            Mathf.Abs(a.chunkY - mainChunk.chunkY) <= chunkDistance;

        private void PruneFarChunks()
        {
            scratch.Clear();
            foreach (var c in currentChunks)
                if (!WithinDistanceOfMainChunk(c))
                    scratch.Add(c);

            foreach (var c in scratch)
            {
                EnqueueDerender(c);
            }
        }

        private bool SpawnTileGameObject(int2 pos, GameObject prefab, Chunk chunk)
        {
            if (prefab == null || chunk.renderedBefore) return false;

            Vector2 circle = UnityEngine.Random.insideUnitCircle * gameObjectVariance;

            GameObject spawned = Instantiate(prefab, new Vector3(pos.x + circle.x + gameObjectOffset.x,
                                            pos.y + circle.y + gameObjectOffset.y,
                                            prefab.transform.position.z), Quaternion.identity, gameObjectParent);
            ChunkObject chunkObject;
            spawned.TryGetComponent(out chunkObject);
            if(chunkObject == null) chunkObject = spawned.AddComponent<ChunkObject>();
            chunk.AddObject(chunkObject);
            chunkObject.SetChunk(chunk, this);

            return true;
        }

        private void SetTilesByChunk(Chunk chunk)
        {
            if (chunk == null) return;

            foreach( int layerId in TileLayerInfo.AllLayerIds)
            {
                var tilemap = tilemapDict[layerId];
                var buf = chunkLayerBuffers[layerId];

                for (int y = 0; y < chunkSize; y++)
                {
                    int gy = y + chunk.origin.y;
                    int rowBase = y * chunkSize;

                    for (int x = 0; x < chunkSize; x++)
                    {
                        int gx = x + chunk.origin.x;
                        int tileId = tileGrid.GetTileId(x, y, layerId);

                        if (useGameObjects)
                        {
                            var prefab = TileObjectInfo.GetGameObject(tileId);
                            if (SpawnTileGameObject(new int2(gx, gy) , prefab, chunk))
                            {
                                buf[rowBase + x] = null;
                                continue;
                            }
                        }

                        buf[rowBase + x] = TileObjectInfo.GetTileBase(tileId);
                    }
                }

                tilemap.SetTilesBlock(chunk.bounds, buf);
            }
        }

        private void RemoveTilesByChunk(Chunk chunk)
        {
            if (chunk == null) return;

            foreach (int layerId in TileLayerInfo.AllLayerIds)
            {
                Tilemap tilemap = tilemapDict[layerId];

                tilemap.SetTilesBlock(chunk.bounds, emptyChunkBuffer);
            }
        }


        private void RenderChunkObjects(Chunk chunk)
        {
            if (chunk == null) return;

            for (int i = chunk.chunkObjects.Count - 1; i >= 0; i--)
            {
                ChunkObject obj = chunk.chunkObjects[i];
                if (obj == null) continue;

                obj.gameObject.SetActive(true);
            }
        }

        private void DerenderChunkObjects(Chunk chunk)
        {
            if (chunk == null) return;

            for (int i = chunk.chunkObjects.Count - 1; i >= 0; i--)
            {
                ChunkObject obj = chunk.chunkObjects[i];
                if (obj == null) continue;

                obj.gameObject.SetActive(false);
            }
        }

        public void RenderChunk(Chunk chunk)
        {
            if(chunk.beingRendered) return;

            chunk.beingRendered = true;
            RenderChunkObjects(chunk);
            SetTilesByChunk(chunk);
            if(!currentChunks.Contains(chunk)) currentChunks.Add(chunk);
            chunk.renderedBefore = true;
        }

        public void DerenderChunk(Chunk chunk)
        {
            if (!chunk.beingRendered) return;

            chunk.beingRendered = false;
            DerenderChunkObjects(chunk);
            RemoveTilesByChunk(chunk);
            currentChunks.Remove(chunk);
        }

        public void EnqueueRender(Chunk c)
        {
            if (c == null || c.beingRendered) return;

            // Cancel Derender of Same Chunk if it exists
            inDerenderQ.Remove(c);

            if (inRenderQ.Add(c))
                renderQueue.Enqueue(c);
        }

        public void EnqueueDerender(Chunk c)
        {
            if (c == null || !c.beingRendered) return;

            // Cancel Render of Same Chunk if it exists
            inRenderQ.Remove(c);

            if (inDerenderQ.Add(c))
                derenderQueue.Enqueue(c);
        }

        private void ProcessQueues()
        {
            // Process Renders
            int n = Mathf.Min(chunkPerFrame, renderQueue.Count);
            for (int i = 0; i < n; i++)
            {
                var c = renderQueue.Dequeue();
                if (!inRenderQ.Remove(c)) continue;   // was cancelled so skip
                RenderChunk(c);
            }

            // If Renders still remain, wait to Derender
            if (renderQueue.Count > 0) return;

            // Process Derenders
            n = Mathf.Min(chunkPerFrame, derenderQueue.Count);
            for (int i = 0; i < n; i++)
            {
                var c = derenderQueue.Dequeue();
                if (!inDerenderQ.Remove(c)) continue; // was cancelled so skip
                DerenderChunk(c);
            }
        }

        public override void CreateWithTilegrid(TileGrid tileGrid)
        {
            if (tileGrid == null || !tileGrid.IsValid)
            {
                Debug.LogError("TileGrid is not valid");
                return;
            }
            this.tileGrid = tileGrid;

            //Determine chunk grid based of chunk size and tilegrid dimensions
            Chunk.chunkSize = chunkSize;

            chunkWidth = Mathf.CeilToInt(tileGrid.width / (float)chunkSize);
            chunkHeight = Mathf.CeilToInt(tileGrid.height / (float)chunkSize);
            int chunkCount = chunkWidth * chunkHeight;
            chunks = new Chunk[chunkCount];

            chunkBounds = new(0, 0, 0, chunkSize, chunkSize, 1);
            emptyChunkBuffer = new TileBase[chunkSize * chunkSize];

            //Create tilemaps for each layer
            foreach (int layerId in TileLayerInfo.AllLayerIds)
            {
                if (!chunkLayerBuffers.ContainsKey(layerId))
                {
                    TileBase[] tileBaseArray = new TileBase[chunkSize * chunkSize];
                    chunkLayerBuffers.Add(layerId, tileBaseArray);
                }

                if (!tilemapDict.ContainsKey(layerId))
                {
                    CreateTileMap(layerId);
                }
            }

            // Create Chunks
            for (int i = 0; i < chunkCount; i++)
            {
                Chunk chunk = new();
                chunk.chunkX = (i % chunkWidth);
                chunk.chunkY = Mathf.FloorToInt(i / (float)chunkWidth);
                chunk.SetOrigin(new(chunk.chunkX * chunkSize, chunk.chunkY * chunkSize));
                chunk.chunkIndex = i;
                chunks[i] = chunk;
            }
        }

        public Chunk GetChunkWorldPos(Vector2 pos)
        {
            int chunkX = Mathf.FloorToInt(pos.x / chunkSize);
            int chunkY = Mathf.FloorToInt(pos.y / chunkSize);

            if (chunkX >= chunkWidth || chunkY >= chunkHeight) return null;

            return GetChunk(chunkX, chunkY); 
        }

        private Chunk GetChunk(int2 pos)
        {
            return GetChunk(pos.x, pos.y);
        }

        private Chunk GetChunk(int x, int y)
        {
            if (x < 0 || y < 0 || x >= chunkWidth || y >= chunkHeight) return null;
            return chunks[x + y * chunkWidth];
        }

        public void SetFollowObject(GameObject follow)
        {
            toFollow = follow;
        }

        private void Update()
        {
            if(randomGenerator != null && toFollow != null)
            {
                // Get Current Chunk
                Chunk currentChunk = GetChunkWorldPos(toFollow.transform.position);
                if (debugging && currentChunk != null) Debug.Log($"Current Chunk - {currentChunk}");
                else if (debugging) Debug.Log($"Current Chunk - Null");

                // If still in same chunk don't do anything
                if (mainChunk == currentChunk || currentChunk == null) return;

                // Render Main Chunk
                mainChunk = currentChunk;
                EnqueueRender(mainChunk);

                // Enqueue Render New Chunks
                for (int x = -chunkDistance; x <= chunkDistance; x++)
                {
                    for (int y = -chunkDistance; y <= chunkDistance; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        Chunk chunk = GetChunk(x + mainChunk.chunkX, y + mainChunk.chunkY);
                        if (chunk == null) continue;

                        if (WithinDistanceOfMainChunk(chunk)) EnqueueRender(chunk);
                    }
                }

                // Enqueue Derender Far Chunks
                PruneFarChunks();
            }
        }

        private void LateUpdate() => ProcessQueues();
    }
}