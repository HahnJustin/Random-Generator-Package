using Codice.CM.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Core
{
    public class TileMask: ICloneable
    {
        public readonly List<TileType> includeList = new();
        public readonly List<TileType> excludeList = new();

        public TileMask(List<TileType> includeList, List<TileType> excludeList)
        {
            this.includeList = includeList;
            this.excludeList = excludeList;
        }

        public object Clone()
        {
            TileMask tilemask = new(includeList.DeepClone(), excludeList.DeepClone());
            return tilemask;
        }
    }
}
