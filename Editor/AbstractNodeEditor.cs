// Editor/AbstractNodeEditor.cs
#if UNITY_EDITOR && ODIN_INSPECTOR
using UnityEditor;
using XNodeEditor;
using static XNodeEditor.NodeEditor;

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
#endif