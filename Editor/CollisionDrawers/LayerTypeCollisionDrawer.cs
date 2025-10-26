#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;   // TileType enum
using System;
using System.Collections.Generic;
using System.Linq;
using Dalichrome.RandomGenerator.UserData;

[CustomPropertyDrawer(typeof(LayerTypeCollisionAttribute))]
public class LayerTypeCollisionDrawer : ResourceCollisionDrawer<TileLayer>
{
    protected override string GetResourceName() => "TileLayer";
    protected override string GetResourceFolderPath() => "TileLayers";
}
#endif
