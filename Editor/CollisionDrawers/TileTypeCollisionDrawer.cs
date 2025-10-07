#if UNITY_EDITOR
using UnityEditor;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;

[CustomPropertyDrawer(typeof(TileTypeCollisionAttribute))]
public class TileTypeCollisionDrawer : TypeResourceCollisionDrawer<TileType, TileObject>
{
    protected override string GetTypeName() => "TileType";
    protected override string GetResourceName() => "TileObject";
    protected override string GetResourceFolderPath() => "TileObjects";
}
#endif
