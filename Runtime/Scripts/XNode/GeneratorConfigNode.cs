using UnityEngine;
using XNode;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator;

[CreateNodeMenu("Generator Node")]
public class GeneratorConfigNode : Node
{
    /* ────── Ports ────── */
    [Input] public TileRegion input;
    [Output] public TileRegion output;

    /* ────── UI State ────── */
    [SerializeField] private bool _enabled = true;
    [SerializeField] private string _displayName = "";
    [SerializeField] private bool _customName = false;

    public bool Enabled { get => _enabled; set => _enabled = value; }
    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value?.Trim();                    // <-- trim
            _customName = !string.IsNullOrEmpty(_displayName);
        }
    }

    public bool HasCustomName => _customName;

    /* ────── Generator config embedded via managed-reference ────── */
    [SerializeReference] private AbstractGeneratorConfig _config;

    public GeneratorType Type => _config != null ? _config.Type : GeneratorType.NA;

    public void SyncNameWithType()      // called by the editor when type changes
    {
        if (!_customName) _displayName = Type.ToString().Replace('_', ' ');
    }

    public AbstractGeneratorConfig CreateConfig()
    {
        if (_config != null)
        {
            _config.Name = DisplayName;
            _config.Enabled = Enabled;
        }
        return _config;
    }

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == nameof(output))
            return Enabled ? GetInputValue<TileRegion>(nameof(input)) : null;

        return null;
    }

    public T GetConfig<T>() where T : AbstractGeneratorConfig => _config as T;

#if UNITY_EDITOR
    private void OnValidate()      // no “override” – works on every version
    {
        UpdatePorts();             // forces port list refresh
    }
#endif
}
