using Dalichrome.RandomGenerator.UserData;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Configs;

#if ODIN_INSPECTOR && UNITY_EDITOR
using Sirenix.OdinInspector.Editor;

public sealed class LayerIdOdinDrawer
    : GenericIdOdinDrawerBase<LayerType, TileLayer, LayerDisplayAttribute>
{
    protected override string ResourcesPath => "TileLayers";
}
#endif