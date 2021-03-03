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

namespace VRtist
{

    public class StraightRay : MonoBehaviour
    {
        [ColorUsage(true, true)]
        public Color defaultColor = new Color(0, 38, 64); // = Color(0.0f, 0.60f, 1.0f) + intensity 6;
        [ColorUsage(true, true)]
        public Color activeColor = new Color(64, 12, 0); // = Color(1.0f, 0.18f, 0.0f) + intensify 6;

        private LineRenderer line = null;
        private Transform endPoint = null;
        private Material rayMat = null;
        private Material endMat = null;

        private float width = 0.0020f;
        private float rayEndPointScale = 0.01f;

        private void Start()
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                Debug.LogWarning("Cannot find the LineRenderer component in the StraightRay");
            }

            endPoint = transform.Find("RayEnd");
            if (endPoint == null)
            {
                Debug.LogWarning("Cannot find the RayEnd object under the StraightRay");
            }

            rayMat = line.material;
            endMat = endPoint.gameObject.GetComponent<MeshRenderer>().material;
        }

        private void Update()
        {
            line.startWidth = width / GlobalState.WorldScale;
            line.endWidth = width / GlobalState.WorldScale;
            endPoint.localScale = Vector3.one * rayEndPointScale / GlobalState.WorldScale;
        }

        public void SetStartPosition(Vector3 start)
        {
            if (line != null)
            {
                line.SetPosition(0, start);
            }
        }

        public void SetEndPosition(Vector3 end)
        {
            if (line != null)
            {
                line.SetPosition(1, end);
            }

            if (endPoint != null)
            {
                endPoint.position = end;
            }
        }

        public void SetPositions(Vector3 begin, Vector3 end)
        {
            if (line != null)
            {
                line.SetPosition(0, begin);
                line.SetPosition(1, end);
            }

            if (endPoint != null)
            {
                endPoint.position = end;
            }
        }

        public void SetDefaultColor()
        {
            SetColor(defaultColor);
        }

        public void SetActiveColor()
        {
            SetColor(activeColor);
        }

        public void SetColor(Color color)
        {
            if (rayMat != null)
            {
                rayMat.SetColor("_EmissiveColor", color);

                // NOTE: only setting the color does not change the shader...
                //rayMat.SetColor("_EmissiveColorLDR", color);
                //rayMat.SetFloat("_EmissiveIntensity", 50.0f); // 50 nits, 6.0 HDR, 8.0 EV100
            }

            if (endMat != null)
            {
                endMat.SetColor("_EmissiveColor", color);

                //endMat.SetColor("_EmissiveColorLDR", color);
                //endMat.SetFloat("_EmissiveIntensity", 50.0f);
            }
        }
    }

}