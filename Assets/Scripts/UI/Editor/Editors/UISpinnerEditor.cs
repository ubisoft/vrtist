using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomEditor(typeof(UISpinner))]
    public class UISpinnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UIElement uiElem = target as UIElement;
            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("Fix Material"))
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
            // Restore the default handles.
            Tools.hidden = false;
        }

        private void OnSceneGUI()
        {
            bool hasUIElementParent = HasUIElemParent();

            UISpinner uiSpinner = target as UISpinner;

            Transform T = uiSpinner.transform;

            Vector3 posRight = T.TransformPoint(new Vector3(+uiSpinner.width, -uiSpinner.height / 2.0f, 0));
            Vector3 posBottom = T.TransformPoint(new Vector3(uiSpinner.width / 2.0f, -uiSpinner.height, 0));
            Vector3 posAnchor = T.TransformPoint(uiSpinner.Anchor);
            Vector3 posSeparation = T.TransformPoint(new Vector3(uiSpinner.margin + (uiSpinner.width - 2 * uiSpinner.margin) * uiSpinner.separationPositionPct, -uiSpinner.height / 2.0f, 0));
            float handleSize = .3f * HandleUtility.GetHandleSize(posAnchor);
            Vector3 snap = Vector3.one * 0.01f;

            EditorGUI.BeginChangeCheck();

            Handles.color = Handles.xAxisColor;
            Vector3 newTargetPosition_right = Handles.FreeMoveHandle(posRight, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.yAxisColor;
            Vector3 newTargetPosition_bottom = Handles.FreeMoveHandle(posBottom, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.zAxisColor;
            Vector3 newTargetPosition_anchor = Handles.FreeMoveHandle(posAnchor, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = new Color(0.8f, 0.4f, 0.1f);
            Vector3 newTargetPosition_separation = Handles.FreeMoveHandle(posSeparation, Quaternion.identity, handleSize, snap, Handles.CubeHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Dimensions");

                Vector3 deltaRight = newTargetPosition_right - posRight;
                Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
                Vector3 deltaAnchor = newTargetPosition_anchor - posAnchor;
                Vector3 deltaSeparation = newTargetPosition_separation - posSeparation;

                if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
                {
                    uiSpinner.Width += deltaRight.x;
                }
                else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
                {
                    uiSpinner.Height += -deltaBottom.y;
                }
                else if (Vector3.SqrMagnitude(deltaSeparation) > Mathf.Epsilon)
                {
                    uiSpinner.separationPositionPct += deltaSeparation.x;
                }
                else if (Vector3.SqrMagnitude(deltaAnchor) > Mathf.Epsilon)
                {
                    Vector3 localDeltaAnchor = T.InverseTransformVector(deltaAnchor);
                    uiSpinner.RelativeLocation += new Vector3(localDeltaAnchor.x, localDeltaAnchor.y, 0.0f);
                }
            }
        }
    }
}
