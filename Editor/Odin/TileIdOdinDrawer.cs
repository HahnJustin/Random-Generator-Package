using Dalichrome.RandomGenerator.UserData;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Configs;


#if ODIN_INSPECTOR && UNITY_EDITOR
using Sirenix.OdinInspector.Editor;

public sealed class TileIdOdinDrawer
    : GenericIdOdinDrawerBase<TileType, TileObject, TileDisplayAttribute>
{
    protected override string ResourcesPath => "TileObjects";
}
#endif