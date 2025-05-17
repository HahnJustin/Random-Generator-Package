using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    public static class Constants
    {
        public static Vector2Int OutsideGridVectorInt { get { return new Vector2Int(-1, -1); } }

        public static int2 OutsideGridInt2 { get { return new int2(-1, -1); } }

        public static int2 Int2Left { get { return new int2(-1,0); } }
        public static int2 Int2Up{ get { return new int2(0, 1); } }
        public static int2 Int2Right { get { return new int2(1, 0); } }
        public static int2 Int2Down { get { return new int2(0, -1); } }


    }
}
