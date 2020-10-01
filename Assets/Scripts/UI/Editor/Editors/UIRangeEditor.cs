using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomEditor(typeof(UIRange))]
    public class UIRangeEditor : Editor
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

            UIRange uiRange = target as UIRange;

            Transform T = uiRange.transform;

            float h2 = -uiRange.height / 2.0f;

            Vector3 posRight = T.TransformPoint(new Vector3(+uiRange.width, h2, 0));
            Vector3 posBottom = T.TransformPoint(new Vector3(uiRange.width / 2.0f, -uiRange.height, 0));
            Vector3 posAnchor = T.TransformPoint(uiRange.Anchor);

            float widthWithoutMargins = (uiRange.width - 2 * uiRange.margin);

            float localLabelPositionEndX = uiRange.margin + widthWithoutMargins * uiRange.labelPositionEnd;
            float localRangePositionBeginX = uiRange.margin + widthWithoutMargins * uiRange.sliderPositionBegin;
            float localRangePositionEndX = uiRange.margin + widthWithoutMargins * uiRange.sliderPositionEnd;

            Vector3 posLabelEnd = T.TransformPoint(new Vector3(localLabelPositionEndX, h2, 0)); 
            Vector3 posRangeBegin = T.TransformPoint(new Vector3(localRangePositionBeginX, h2, 0));
            Vector3 posRangeEnd = T.TransformPoint(new Vector3(localRangePositionEndX, h2, 0));

            float localRangeWidth = localRangePositionEndX - localRangePositionBeginX;

            float pctRangeMin = (uiRange.currentRange.x - uiRange.globalRange.x) / (uiRange.globalRange.y - uiRange.globalRange.x);
            float pctRangeMax = (uiRange.currentRange.y - uiRange.globalRange.x) / (uiRange.globalRange.y - uiRange.globalRange.x);
            float pctRangeMid = (pctRangeMin + pctRangeMax) / 2.0f;

            float localPosRangeMinX = localRangePositionBeginX + localRangeWidth * pctRangeMin;
            float localPosRangeMaxX = localRangePositionBeginX + localRangeWidth * pctRangeMax;
            float localPosRangeMidX = localRangePositionBeginX + localRangeWidth * pctRangeMid;

            Vector3 posRangeMin = T.TransformPoint(new Vector3(localPosRangeMinX, h2, 0));
            Vector3 posRangeMax = T.TransformPoint(new Vector3(localPosRangeMaxX, h2, 0));
            Vector3 posRangeMid = T.TransformPoint(new Vector3(localPosRangeMidX, h2, 0));

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
            Vector3 newTargetPosition_labelEnd = Handles.FreeMoveHandle(posLabelEnd, Quaternion.identity, handleSize, snap, Handles.CubeHandleCap);

            Handles.color = new Color(0.8f, 0.4f, 0.1f);
            Vector3 newTargetPosition_sliderBegin = Handles.FreeMoveHandle(posRangeBegin, Quaternion.identity, handleSize, snap, Handles.CubeHandleCap);

            Handles.color = new Color(0.8f, 0.4f, 0.1f);
            Vector3 newTargetPosition_sliderEnd = Handles.FreeMoveHandle(posRangeEnd, Quaternion.identity, handleSize, snap, Handles.CubeHandleCap);

            Handles.color = Handles.xAxisColor;
            Vector3 newTargetPosition_rangeMin = Handles.FreeMoveHandle(posRangeMin, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Handles.yAxisColor;
            Vector3 newTargetPosition_rangeMax = Handles.FreeMoveHandle(posRangeMax, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            Handles.color = Color.white;
            Vector3 newTargetPosition_rangeMid = Handles.FreeMoveHandle(posRangeMid, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Dimensions");

                Vector3 deltaRight = newTargetPosition_right - posRight;
                Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
                Vector3 deltaAnchor = newTargetPosition_anchor - posAnchor;
                Vector3 deltaLabelEnd = newTargetPosition_labelEnd - posLabelEnd;
                Vector3 deltaRangeBegin = newTargetPosition_sliderBegin - posRangeBegin;
                Vector3 deltaRangeEnd = newTargetPosition_sliderEnd - posRangeEnd;
                Vector3 deltaRangeMin = newTargetPosition_rangeMin - posRangeMin;
                Vector3 deltaRangeMax = newTargetPosition_rangeMax - posRangeMax;
                Vector3 deltaRangeMid = newTargetPosition_rangeMid - posRangeMid;

                if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
                {
                    uiRange.Width += deltaRight.x;
                }
                else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
                {
                    uiRange.Height += -deltaBottom.y;
                }
                else if (Vector3.SqrMagnitude(deltaLabelEnd) > Mathf.Epsilon)
                {
                    uiRange.LabelPositionEnd += deltaLabelEnd.x;
                }
                else if (Vector3.SqrMagnitude(deltaRangeBegin) > Mathf.Epsilon)
                {
                    uiRange.RangePositionBegin += deltaRangeBegin.x;
                }
                else if (Vector3.SqrMagnitude(deltaRangeEnd) > Mathf.Epsilon)
                {
                    uiRange.RangePositionEnd += deltaRangeEnd.x;
                }
                else if (Vector3.SqrMagnitude(deltaRangeMin) > Mathf.Epsilon)
                {
                    Vector3 localDeltaRangeMin = T.InverseTransformVector(deltaRangeMin);
                    float newLocalPosRangeMinX = localPosRangeMinX + localDeltaRangeMin.x;
                    float newPctRangeMin = (newLocalPosRangeMinX - localRangePositionBeginX) / localRangeWidth;
                    float newValueRangeMin = uiRange.globalRange.x + newPctRangeMin * (uiRange.globalRange.y - uiRange.globalRange.x);

                    uiRange.CurrentRange = new Vector2(newValueRangeMin, uiRange.CurrentRange.y);
                }
                else if (Vector3.SqrMagnitude(deltaRangeMax) > Mathf.Epsilon)
                {
                    Vector3 localDeltaRangeMax = T.InverseTransformVector(deltaRangeMax);
                    float newLocalPosRangeMaxX = localPosRangeMaxX + localDeltaRangeMax.x;
                    float newPctRangeMax = (newLocalPosRangeMaxX - localRangePositionBeginX) / localRangeWidth;
                    float newValueRangeMax = uiRange.globalRange.x + newPctRangeMax * (uiRange.globalRange.y - uiRange.globalRange.x);

                    uiRange.CurrentRange = new Vector2(uiRange.CurrentRange.x, newValueRangeMax);
                }
                else if (Vector3.SqrMagnitude(deltaRangeMid) > Mathf.Epsilon)
                {
                    Vector3 localDeltaRangeMid = T.InverseTransformVector(deltaRangeMid);

                    float newLocalPosRangeMinX = localPosRangeMinX + localDeltaRangeMid.x;
                    float newPctRangeMin = (newLocalPosRangeMinX - localRangePositionBeginX) / localRangeWidth;
                    float newValueRangeMin = uiRange.globalRange.x + newPctRangeMin * (uiRange.globalRange.y - uiRange.globalRange.x);

                    float newLocalPosRangeMaxX = localPosRangeMaxX + localDeltaRangeMid.x;
                    float newPctRangeMax = (newLocalPosRangeMaxX - localRangePositionBeginX) / localRangeWidth;
                    float newValueRangeMax = uiRange.globalRange.x + newPctRangeMax * (uiRange.globalRange.y - uiRange.globalRange.x);

                    uiRange.CurrentRange = new Vector2(newValueRangeMin, newValueRangeMax);
                }
                else if (Vector3.SqrMagnitude(deltaAnchor) > Mathf.Epsilon)
                {
                    Vector3 localDeltaAnchor = T.InverseTransformVector(deltaAnchor);
                    uiRange.RelativeLocation += new Vector3(localDeltaAnchor.x, localDeltaAnchor.y, 0.0f);
                }
            }
        }
    }
}
