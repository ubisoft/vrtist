using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(ComputeGrassPainter))]
public class ComputeGrassPainterEditor : Editor
{
    ComputeGrassPainter grassPainter;
    readonly string[] toolbarStrings = { "Add", "Remove", "Edit" };

    private void OnEnable()
    {
        grassPainter = (ComputeGrassPainter)target;
    }

    void OnSceneGUI()
    {
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
        Handles.color = new Color(0, 0.5f, 0.5f, 0.4f);
        Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
        if (grassPainter.toolbarInt == 1)
        {
            Handles.color = Color.red;
            Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
            Handles.color = new Color(0.5f, 0f, 0f, 0.4f);
            Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
        }
        if (grassPainter.toolbarInt == 2)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
            Handles.color = new Color(0.5f, 0.5f, 0f, 0.4f);
            Handles.DrawSolidDisc(grassPainter.hitPosGizmo, grassPainter.hitNormal, grassPainter.brushSize);
        }
    }

    public override void OnInspectorGUI()
    {
        //EditorGUILayout.LabelField("Grass Limit", EditorStyles.boldLabel);
        //EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField(grassPainter.i.ToString() + "/", EditorStyles.label);
        //grassPainter.grassLimit = EditorGUILayout.IntField(grassPainter.grassLimit);
        //EditorGUILayout.EndHorizontal();
        //EditorGUILayout.Space();
        EditorGUILayout.LabelField("Paint Status (Right-Mouse Button to paint)", EditorStyles.boldLabel);
        grassPainter.toolbarInt = GUILayout.Toolbar(grassPainter.toolbarInt, toolbarStrings);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
        LayerMask tempMask = EditorGUILayout.MaskField("Hit Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(grassPainter.hitMask), InternalEditorUtility.layers);
        grassPainter.hitMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
        LayerMask tempMask2 = EditorGUILayout.MaskField("Painting Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(grassPainter.paintMask), InternalEditorUtility.layers);
        grassPainter.paintMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask2);

        grassPainter.brushSize = EditorGUILayout.Slider("Brush Size", grassPainter.brushSize, 0.1f, 10f);
        grassPainter.density = EditorGUILayout.Slider("Density", grassPainter.density, 0.1f, 10f);
        grassPainter.normalLimit = EditorGUILayout.Slider("Normal Limit", grassPainter.normalLimit, 0f, 1f);


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Width and Length ", EditorStyles.boldLabel);
        grassPainter.widthMultiplier = EditorGUILayout.Slider("Grass Width", grassPainter.widthMultiplier, 0f, 2f);
        grassPainter.heightMultiplier = EditorGUILayout.Slider("Grass Length", grassPainter.heightMultiplier, 0f, 2f);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
        grassPainter.adjustedColor = EditorGUILayout.ColorField("Brush Color", grassPainter.adjustedColor);
        EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
        grassPainter.rangeR = EditorGUILayout.Slider("Red", grassPainter.rangeR, 0f, 1f);
        grassPainter.rangeG = EditorGUILayout.Slider("Green", grassPainter.rangeG, 0f, 1f);
        grassPainter.rangeB = EditorGUILayout.Slider("Blue", grassPainter.rangeB, 0f, 1f);

        if (GUILayout.Button("Clear Mesh"))
        {
            if (EditorUtility.DisplayDialog("Clear Painted Mesh?",
               "Are you sure you want to clear the mesh?", "Clear", "Don't Clear"))
            {
                grassPainter.ClearMesh();
            }
        }
    }
}
