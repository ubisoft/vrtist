using UnityEngine;
using System.Collections;
using UnityEditor;

namespace VRtist
{

    [CustomEditor(typeof(UIPanel))]
    public class UIPanelEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UIElement uiElem = target as UIElement;
            if (GUILayout.Button("Reset Material"))
            {
                uiElem.ResetMaterial();
            }
        }

        private bool HasUIElemParent()
        {
            UIElement uiElem = (UIElement)target;
            UIElement parentElem = uiElem.transform.parent ? uiElem.transform.parent.gameObject.GetComponent<UIElement>() : null;
            return parentElem != null;
        }

        private void OnEnable()
        {
            Tools.hidden = true;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        private void OnSceneGUI()
        {
            bool hasUIElementParent = HasUIElemParent();

            UIPanel uiPanel = target as UIPanel;

            Transform T = uiPanel.transform;

            Vector3 posRight = T.TransformPoint(new Vector3(uiPanel.width, -uiPanel.height / 2.0f, 0));
            Vector3 posBottom = T.TransformPoint(new Vector3(uiPanel.width / 2.0f, -uiPanel.height, 0));
            Vector3 posAnchor = T.TransformPoint(uiPanel.Anchor);
            float handleSize = .3f * HandleUtility.GetHandleSize(posAnchor);
            Vector3 snap = Vector3.one * 0.01f;

            EditorGUI.BeginChangeCheck();

            Handles.color = Handles.xAxisColor;
            Vector3 newTargetPosition_right = Handles.FreeMoveHandle(posRight, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.yAxisColor;
            Vector3 newTargetPosition_bottom = Handles.FreeMoveHandle(posBottom, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.zAxisColor;
            //Vector3 newTargetPosition_anchor = hasUIElementParent ? Handles.FreeMoveHandle(posAnchor, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap) : posAnchor;
            Vector3 newTargetPosition_anchor = Handles.FreeMoveHandle(posAnchor, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Dimensions");

                Vector3 deltaRight = newTargetPosition_right - posRight;
                Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
                Vector3 deltaAnchor = newTargetPosition_anchor - posAnchor;

                if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
                {
                    //Vector3 localDeltaRight = T.InverseTransformPoint(deltaRight);
                    //uiPanel.RelativeLocation += new Vector3(deltaRight.x / 2.0f, 0.0f, 0.0f);
                    uiPanel.Width += deltaRight.x;
                }
                else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
                {
                    //Vector3 localDeltaBottom = T.InverseTransformPoint(deltaBottom);
                    //uiPanel.RelativeLocation += new Vector3(0.0f, deltaBottom.y / 2.0f, 0.0f);
                    uiPanel.Height += -deltaBottom.y;
                }
                else if (Vector3.SqrMagnitude(deltaAnchor) > Mathf.Epsilon)
                {
                    Vector3 localDeltaAnchor = T.InverseTransformVector(deltaAnchor);
                    uiPanel.RelativeLocation += new Vector3(localDeltaAnchor.x, localDeltaAnchor.y, 0.0f);
                }
            }
        }
    }
}
