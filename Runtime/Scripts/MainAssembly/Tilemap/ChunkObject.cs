using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    public class ChunkObject : MonoBehaviour
    {
        [SerializeField] private bool stationaryObject = false;

        [SerializeField] private float minMoveSqr = 0.25f;
        private Vector3 lastCheckPos;
        private Chunk chunk;
        private TilemapChunker chunker;

        void Awake() => lastCheckPos = transform.position;

        private void Start()
        {
            if (stationaryObject) enabled = false;
        }

        public void SetChunk(Chunk chunk, TilemapChunker chunker)
        {
            this.chunk = chunk;
            this.chunker = chunker;
        }

        private void LateUpdate()
        {
            var p = transform.position;
            if ((p - lastCheckPos).sqrMagnitude < minMoveSqr) return;
            lastCheckPos = p;

            var newChunk = chunker.GetChunkWorldPos(p);
            if (newChunk != chunk)
            {
                var old = chunk;
                if (old != null) old.RemoveObject(this);
                chunk = newChunk;
                if (newChunk != null) newChunk.AddObject(this);
                //OnChunkChanged?.Invoke(this, old, newChunk);
            }
        }
    }
}
