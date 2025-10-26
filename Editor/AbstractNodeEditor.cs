// Editor/AbstractNodeEditor.cs
#if UNITY_EDITOR && ODIN_INSPECTOR
using XNodeEditor;
using Dalichrome.RandomGenerator.Nodes;

namespace Dalichrome.RandomGenerator.Editor
{
    [CustomNodeEditor(typeof(AbstractNode))]
    public abstract class AbstractNodeEditor : NodeEditor
    {
        void OnDisable()          // Unity message, no "override"
        {
            // NodeEditorBase stores its PropertyTree in 'objectTree'
            if (objectTree != null)
                objectTree.Dispose();
        }

    }
}
#endif