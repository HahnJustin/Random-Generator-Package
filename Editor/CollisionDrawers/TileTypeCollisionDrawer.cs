#if UNITY_EDITOR
using UnityEditor;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.UserData;

[CustomPropertyDrawer(typeof(TileTypeCollisionAttribute))]
public class TileTypeCollisionDrawer : ResourceCollisionDrawer<TileObject>
{
    protected override string GetResourceName() => "TileObject";
    protected override string GetResourceFolderPath() => "TileObjects";
}
#endif
