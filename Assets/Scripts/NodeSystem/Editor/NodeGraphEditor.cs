using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeGraph))]
public class NodeGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NodeGraph graph = (NodeGraph)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Node List (quét toàn scene)"))
        {
            graph.RefreshNodeList();
            EditorUtility.SetDirty(graph);
        }
    }
}
