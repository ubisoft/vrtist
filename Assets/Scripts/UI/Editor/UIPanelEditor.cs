using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(UIPanel))]
public class SomeScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.HelpBox("Rebuild the mesh of the UIPanel", MessageType.Info);

        UIPanel uiPanel = (UIPanel)target;
        if (GUILayout.Button("Rebuild"))
        {
            uiPanel.RebuildMesh();
        }
    }
}
