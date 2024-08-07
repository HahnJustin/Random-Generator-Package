using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public interface IOccupanceUtil
    {
        public abstract int IsOccupied(Vector2Int position);

        public abstract int IsOccupied(int x, int y);

        public abstract int IsOccupied(Tile tile);

        public abstract void Fill(Tile tile);

        public abstract bool GetIfOccupiedTileNextToPosition(int x, int y, int movement = 1);

        public abstract Vector2Int GetPointInDirection(Vector2Int point, Direction direction, int movement = 1);
    }
}