using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIButton))]
public class UIButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        UIButton uiButton = target as UIButton;

        Transform T = uiButton.transform;

        Vector3 posLeft = T.TransformPoint(new Vector3(-uiButton.width / 2.0f, 0, 0));
        Vector3 posRight = T.TransformPoint(new Vector3(+uiButton.width / 2.0f, 0, 0));
        Vector3 posBottom = T.TransformPoint(new Vector3(0, -uiButton.height / 2.0f, 0));
        Vector3 posTop = T.TransformPoint(new Vector3(0, +uiButton.height / 2.0f, 0));
        Vector3 posAnchor = T.TransformPoint(uiButton.anchor);
        float handleSize = uiButton.thickness;
        Vector3 snap = Vector3.one * 0.1f;

        EditorGUI.BeginChangeCheck();

        Handles.color = Handles.xAxisColor;
        Vector3 newTargetPosition_left = Handles.FreeMoveHandle(posLeft, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);
        Vector3 newTargetPosition_right = Handles.FreeMoveHandle(posRight, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

        Handles.color = Handles.yAxisColor;
        Vector3 newTargetPosition_bottom = Handles.FreeMoveHandle(posBottom, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);
        Vector3 newTargetPosition_top = Handles.FreeMoveHandle(posTop, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

        Handles.color = Handles.zAxisColor;
        Vector3 newTargetPosition_anchor = Handles.FreeMoveHandle(posAnchor, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Change Dimensions");

            Vector3 deltaLeft = newTargetPosition_left - posLeft;
            Vector3 deltaRight = newTargetPosition_right - posRight;
            Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
            Vector3 deltaTop = newTargetPosition_top - posTop;
            Vector3 deltaAnchor = newTargetPosition_anchor - posAnchor; // TODO: delta X, delta Y ?

            if (Vector3.SqrMagnitude(deltaLeft) > Mathf.Epsilon)
            {
                uiButton.Width = 2.0f * Vector3.Magnitude(T.InverseTransformPoint(newTargetPosition_left));
            }
            else if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
            {
                uiButton.Width = 2.0f * Vector3.Magnitude(T.InverseTransformPoint(newTargetPosition_right));
            }
            else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
            {
                uiButton.Height = 2.0f * Vector3.Magnitude(T.InverseTransformPoint(newTargetPosition_bottom));
            }
            else if (Vector3.SqrMagnitude(deltaTop) > Mathf.Epsilon)
            {
                uiButton.Height = 2.0f * Vector3.Magnitude(T.InverseTransformPoint(newTargetPosition_top));
            }
            else if (Vector3.SqrMagnitude(deltaAnchor) > Mathf.Epsilon)
            {
                // TODO
                //uiButton.Anchor = 2.0f * Vector3.Magnitude(T.InverseTransformPoint(newTargetPosition_top));
            }
        }
    }
}
