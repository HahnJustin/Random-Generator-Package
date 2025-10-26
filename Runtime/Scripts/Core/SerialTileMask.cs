using Dalichrome.RandomGenerator.Random;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Core
{
    [Serializable]
    public class SerialTileMask : ITileMask
    {
        [ReadOnly] public List<int> includeList;
        [ReadOnly] public List<int> excludeList;

        public bool IsValid { get; internal set; }

        public bool IsIncludingTiles
        {
            get
            {
                if (!IsValid) return false;
                return includeList.Count > 0;
            }
        }
        public bool IsExcludingTiles
        {
            get
            {
                if (!IsValid) return false;
                return excludeList.Count > 0;
            }
        }

        public SerialTileMask()
        {
            this.includeList = new();
            this.excludeList = new();
        }

        public SerialTileMask(List<int> includeList, List<int> excludeList)
        {
            IsValid = true;

            this.includeList = new(includeList);
            this.excludeList = new(excludeList);

            foreach (var t in includeList)
                this.includeList.Add((int)t);

            foreach (var t in excludeList)
                this.excludeList.Add((int)t);
        }

        public bool CanModifyTile(List<int> ids)
        {
            if (!IsValid) return true;

            bool included = false;
            bool excluded = false;

            foreach (int id in includeList)
            {
                if (ids.Contains(id))
                {
                    included = true;
                    break;
                }
            }

            foreach (int id in excludeList)
            {
                if (ids.Contains(id))
                {
                    excluded = true;
                    break;
                }
            }

            if (IsExcludingTiles && excluded) return false;
            else if (IsIncludingTiles && included) return true;
            else return !IsIncludingTiles;
        }

        public bool CanModifyTileId(int id)
        {
            if (!IsValid) return true;

            bool included = includeList.Contains(id);
            bool excluded = excludeList.Contains(id);

            if (IsExcludingTiles && excluded) return false;
            else if (IsIncludingTiles && included) return true;
            else return !IsIncludingTiles;
        }


        public ITileMask DeepClone()
        {
            var clone = new SerialTileMask
            {
                includeList = new(this.includeList),
                excludeList = new(this.excludeList),
                IsValid = this.IsValid
            };

            return clone;
        }
    }
}
