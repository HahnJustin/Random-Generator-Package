using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Databases
{
    [CreateAssetMenu(menuName = "Databases/TileInfoDatabase")]
    public class TileInfoDatabase : Database<TileType, TileInfo>
    {
    }
}
