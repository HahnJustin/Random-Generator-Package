// NodeEditorReloadHook.cs
#if UNITY_EDITOR && ODIN_INSPECTOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;

[InitializeOnLoad]
static class NodeEditorReloadHook
{
    internal static readonly HashSet<AbstractNode> Nodes = new();
    internal static readonly HashSet<PropertyTree> LiveTrees = new();

    static NodeEditorReloadHook()
    {
        AssemblyReloadEvents.beforeAssemblyReload += DisposeAll;
        EditorApplication.quitting += DisposeAll;
    }

    static void DisposeAll()
    {
        foreach (PropertyTree t in LiveTrees) t?.Dispose();
        LiveTrees.Clear();
    }
}
#endif
