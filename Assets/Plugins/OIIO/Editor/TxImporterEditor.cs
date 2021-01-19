using UnityEditor;

using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(TxImporter))]
public class TxImporterEditor : UnityEditor.AssetImporters.ScriptedImporterEditor
{
    public override void OnInspectorGUI()
    {
        var dimensions = new GUIContent("Dimensions");
        var prop = serializedObject.FindProperty("imageDimensions");
        EditorGUILayout.PropertyField(prop, dimensions);
        base.ApplyRevertGUI();
    }
}
