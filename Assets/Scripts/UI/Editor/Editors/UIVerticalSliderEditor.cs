/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [CustomEditor(typeof(UIVerticalSlider))]
    public class UIVerticalSliderEditor : Editor
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

            UIVerticalSlider uiSlider = target as UIVerticalSlider;

            Transform T = uiSlider.transform;

            Vector3 posRight = T.TransformPoint(new Vector3(+uiSlider.width, -uiSlider.height / 2.0f, 0));
            Vector3 posBottom = T.TransformPoint(new Vector3(uiSlider.width / 2.0f, -uiSlider.height, 0));
            Vector3 posAnchor = T.TransformPoint(uiSlider.Anchor);
            Vector3 posSliderBegin = T.TransformPoint(new Vector3(uiSlider.width / 2.0f, -uiSlider.height + uiSlider.margin + (uiSlider.height - 2 * uiSlider.margin) * uiSlider.sliderPositionBegin, 0));
            Vector3 posSliderEnd = T.TransformPoint(new Vector3(uiSlider.width / 2.0f, -uiSlider.height + uiSlider.margin + (uiSlider.height - 2 * uiSlider.margin) * uiSlider.sliderPositionEnd, 0));
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
            Vector3 newTargetPosition_sliderBegin = Handles.FreeMoveHandle(posSliderBegin, Quaternion.identity, handleSize, snap, Handles.CubeHandleCap);

            Handles.color = new Color(0.8f, 0.4f, 0.1f);
            Vector3 newTargetPosition_sliderEnd = Handles.FreeMoveHandle(posSliderEnd, Quaternion.identity, handleSize, snap, Handles.CubeHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Dimensions");

                Vector3 deltaRight = newTargetPosition_right - posRight;
                Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
                Vector3 deltaAnchor = newTargetPosition_anchor - posAnchor;
                Vector3 deltaSliderBegin = newTargetPosition_sliderBegin - posSliderBegin;
                Vector3 deltaSliderEnd = newTargetPosition_sliderEnd - posSliderEnd;

                if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
                {
                    uiSlider.Width += deltaRight.x;
                }
                else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
                {
                    uiSlider.Height += -deltaBottom.y;
                }
                else if (Vector3.SqrMagnitude(deltaSliderBegin) > Mathf.Epsilon)
                {
                    uiSlider.SliderPositionBegin += deltaSliderBegin.y;
                }
                else if (Vector3.SqrMagnitude(deltaSliderEnd) > Mathf.Epsilon)
                {
                    uiSlider.SliderPositionEnd += deltaSliderEnd.y;
                }
                else if (Vector3.SqrMagnitude(deltaAnchor) > Mathf.Epsilon)
                {
                    Vector3 localDeltaAnchor = T.InverseTransformVector(deltaAnchor);
                    uiSlider.RelativeLocation += new Vector3(localDeltaAnchor.x, localDeltaAnchor.y, 0.0f);
                }
            }
        }
    }
}
