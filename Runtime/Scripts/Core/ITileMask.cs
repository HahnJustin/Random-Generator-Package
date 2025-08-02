using Dalichrome.RandomGenerator.Random;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace Dalichrome.RandomGenerator.Core
{
    public interface ITileMask
    {
        public bool IsValid { get; }

        public bool IsIncludingTiles { get; }
        public bool IsExcludingTiles { get; }

        public bool CanModifyTile(Tile tile);

        public ITileMask DeepClone();
        
    }
}
