using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrassRenderer))]
public class GrassPainterRenderer : Editor
{
    GrassRenderer grassRenderer;
    
    private void OnEnable()
    {
        grassRenderer = (GrassRenderer)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Fix Mesh Ref"))
        {
            grassRenderer.FixMeshRef();
        }
    }
}
