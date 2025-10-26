using UnityEngine;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator
{
    public class Chunk
    {
        public static int chunkSize = 16;

        public List<ChunkObject> chunkObjects = new();
        public Vector2Int origin;
        public int chunkIndex;
        public int chunkX;
        public int chunkY;
        public bool beingRendered = false;
        public bool renderedBefore = false;

        public BoundsInt bounds;

        public void SetOrigin(Vector2Int origin)
        {
            this.origin = origin;
            bounds = new BoundsInt(origin.x, origin.y, 0, chunkSize, chunkSize, 1);
        }

        public void AddObject(ChunkObject chunkObj)
        {
            chunkObjects.Add(chunkObj);
        }

        public bool RemoveObject(ChunkObject chunkObj)
        {
            return chunkObjects.Remove(chunkObj);
        }

        public override string ToString()
        {
            return $"Chunk X:{chunkX} Y:{chunkY} Rendering:{beingRendered} Origin:{origin} Object#:{chunkObjects.Count}";
        }
    }
}