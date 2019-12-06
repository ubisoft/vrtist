using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [CustomEditor(typeof(UI3DObject))]
    public class UI3DObjectditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private bool HasUIElemParent()
        {
            UIElement uiElem = (UIElement)target;
            UIElement parentElem = uiElem.transform.parent ? uiElem.transform.parent.gameObject.GetComponent<UIElement>() : null;
            return parentElem != null;
        }

        private void OnEnable()
        {
            //if (HasUIElemParent())
            {
                // Hide the default handles, so that they don't get in the way.
                // But not if this panel is a top level GUI widget.
                Tools.hidden = true;
            }
        }

        private void OnDisable()
        {
            // Restore the default handles.
            Tools.hidden = false;
        }

        private void OnSceneGUI()
        {
            bool hasUIElementParent = HasUIElemParent();

            UI3DObject ui3DObject = target as UI3DObject;

            Transform T = ui3DObject.transform;

            Vector3 posAnchor = T.TransformPoint(ui3DObject.Anchor);
            Vector3 posRight = T.TransformPoint(new Vector3(+ui3DObject.width, -ui3DObject.height / 2.0f, -ui3DObject.depth / 2.0f));
            Vector3 posBottom = T.TransformPoint(new Vector3(ui3DObject.width / 2.0f, -ui3DObject.height, -ui3DObject.depth / 2.0f));
            Vector3 posFront = T.TransformPoint(new Vector3(ui3DObject.width / 2.0f, -ui3DObject.height / 2.0f, -ui3DObject.depth));
            float handleSize = .3f * HandleUtility.GetHandleSize(posAnchor);
            Vector3 snap = Vector3.one * 0.01f;

            EditorGUI.BeginChangeCheck();

            Handles.color = new Color(1.0f, 1.0f, 0.0f);
            Vector3 newTargetPosition_anchor = Handles.FreeMoveHandle(posAnchor, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);
            
            Handles.color = Handles.xAxisColor;
            Vector3 newTargetPosition_right = Handles.FreeMoveHandle(posRight, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.yAxisColor;
            Vector3 newTargetPosition_bottom = Handles.FreeMoveHandle(posBottom, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.zAxisColor;
            Vector3 newTargetPosition_front = Handles.FreeMoveHandle(posFront, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Dimensions");

                Vector3 deltaAnchor = newTargetPosition_anchor - posAnchor;
                Vector3 deltaRight = newTargetPosition_right - posRight;
                Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
                Vector3 deltaFront = newTargetPosition_front - posFront;
                
                if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
                {
                    ui3DObject.Width += deltaRight.x;
                }
                else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
                {
                    ui3DObject.Height += -deltaBottom.y;
                }
                else if (Vector3.SqrMagnitude(deltaFront) > Mathf.Epsilon)
                {
                    ui3DObject.Depth += -deltaFront.z;
                }
                else if (Vector3.SqrMagnitude(deltaAnchor) > Mathf.Epsilon)
                {
                    Vector3 localDeltaAnchor = T.InverseTransformVector(deltaAnchor);
                    ui3DObject.RelativeLocation += new Vector3(localDeltaAnchor.x, localDeltaAnchor.y, 0.0f);
                }
            }
        }
    }
}
