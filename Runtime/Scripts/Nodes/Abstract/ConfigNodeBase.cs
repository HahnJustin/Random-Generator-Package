// ConfigNodeBase.cs
using Dalichrome.RandomGenerator.Configs;
using UnityEngine;
using XNode;

namespace Dalichrome.RandomGenerator.Nodes
{

    /// <summary>Node with an embedded managed-reference config object.</summary>
    public abstract class ConfigNodeBase<TCfg> : Node, IConfigNode
        where TCfg : AbstractConfig                        // ScriptableObject OR plain class
    {
        /* ───── UI state ───── */
        [SerializeField] private bool _enabled = true;
        [SerializeField] private string _displayName = "";
        [SerializeField] private bool _customName = false;

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value?.Trim(); _customName = !string.IsNullOrEmpty(_displayName); }
        }
        public bool HasCustomName => _customName;

        /* ───── Config (managed-reference!) ───── */
        [SerializeReference] private TCfg _config;
        public AbstractConfig Config { get { return _config; } }     // for subclasses

        /// <summary>The concrete base-class you want to appear in the drop-down
        /// (e.g. typeof(AbstractGeneratorConfig) or typeof(AbstractRegionSplitterConfig))</summary>
        public abstract System.Type ConfigBaseType { get; }

        /// <summary>Enum (or any int) that identifies the node type – only used
        /// for pretty header colours.</summary>
        public abstract int PaletteSeed { get; }

        /// <summary>Called by the editor after the user switches the config type.</summary>
        public virtual void SyncNameWithType()
        {
            if (_customName || _config == null) return;
            DisplayName = _config.GetType().Name.Replace("Config", "").Replace('_', ' ');
        }

#if UNITY_EDITOR
        private void OnValidate() => UpdatePorts();   // keep XNode happy in Edit-mode
#endif
    }
}
