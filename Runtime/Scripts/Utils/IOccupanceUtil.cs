using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public interface IOccupanceUtil
    {
        public abstract int IsOccupied(Vector2Int position);

        public abstract int IsOccupied(int x, int y);

        public abstract int IsOccupied(int2 position);

        public abstract bool GetIfOccupiedTileNextToPosition(int x, int y, int movement = 1);

        public abstract OccupanceData GetOccupanceData();
    }
}