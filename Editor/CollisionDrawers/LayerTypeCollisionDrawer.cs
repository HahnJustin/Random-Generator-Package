#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;   // TileType enum
using System;
using System.Collections.Generic;
using System.Linq;
using Dalichrome.RandomGenerator.UserData;

[CustomPropertyDrawer(typeof(LayerTypeCollisionAttribute))]
public class LayerTypeCollisionDrawer : TypeResourceCollisionDrawer<LayerType, TileLayer>
{
    protected override string GetTypeName() => "LayerType";
    protected override string GetResourceName() => "TileLayer";
    protected override string GetResourceFolderPath() => "TileLayers";
}
#endif
