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

using UnityEngine;

namespace VRtist
{
    public class UIRay : MonoBehaviour
    {
        [GradientUsage(true)]
        public Gradient volumeColor = default;
        [GradientUsage(true)]
        public Gradient widgetColor = default;
        [GradientUsage(true)]
        public Gradient panelColor = default;
        [GradientUsage(true)]
        public Gradient handleColor = default;

        //[ColorUsage(true, true)]
        //public Color volumeColor = new Color(0, 38, 64);
        //[ColorUsage(true, true)]
        //public Color widgetColor = new Color(64, 12, 0);
        //[ColorUsage(true, true)]
        //public Color panelColor = new Color(0, 64, 12);
        //[ColorUsage(true, true)]
        //public Color handleColor = new Color(12, 64, 0);

        private LineRenderer line = null;
        private Material rayMat = null;

        public Transform endPoint = null;
        
        [Range(0, 1)]
        public float endPointColorGrandientPct = 0.6f;

        [Range(1, 100)]
        public float startWidth = 100;

        [Range(1, 100)]
        public float endWidth = 1;

        private float f = 10000f;

        private void Start()
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                Debug.LogWarning("Cannot find the LineRenderer component in the UIRay");
            }

            rayMat = line.material;
        }

        //public void SetStartPosition(Vector3 start)
        //{
        //    if (line != null)
        //    {
        //        line.SetPosition(0, start);
        //        Vector3 end = line.GetPosition(10);

        //        for (int i = 1; i < 10; ++i)
        //        {
        //            float pct = ((float)i / 10.0f);
        //            line.SetPosition(i, start + (end - start) * pct);
        //        }

        //        line.startWidth = startWidth / f;
        //        line.endWidth = endWidth / f;
        //    }
        //}

        //public void SetEndPosition(Vector3 end)
        //{
        //    if (line != null)
        //    {
        //        line.SetPosition(10, end);
        //        Vector3 start = line.GetPosition(0);

        //        for (int i = 1; i < 10; ++i)
        //        {
        //            float pct = ((float)i / 10.0f);
        //            line.SetPosition(i, start + (end - start) * pct);
        //        }

        //        line.startWidth = startWidth / f;
        //        line.endWidth = endWidth / f;
        //    }

        //    if (endPoint != null)
        //    {
        //        endPoint.position = end;
        //    }
        //}

        public void SetParameters(Vector3 start, Vector3 end, Vector3 startTangent)
        {
            if (line != null)
            {
                Vector3 middlePoint = (start + end) / 2.0f;
                Vector3 middleNormal = Vector3.Normalize(start - middlePoint);
                Plane middlePlane = new Plane(middleNormal, middlePoint);
                Ray ray = new Ray(start, startTangent);
                float enter = 0.0f;
                middlePlane.Raycast(ray, out enter);
                Vector3 intersectionPoint = ray.GetPoint(enter);

                Vector3 P0 = start;
                //Vector3 P1 = intersectionPoint;
                Vector3 P1 = start + 0.6f * (intersectionPoint - start);
                Vector3 P2 = end;

                for (int i = 0; i < 11; ++i)
                {
                    float t = ((float)i / 10.0f);
                    Vector3 pos = P2 * t * t + P1 * 2 * t * (1.0f - t) + P0 * (1.0f - t) * (1.0f - t);
                    line.SetPosition(i, pos);
                }

                float scale = 1f / GlobalState.WorldScale;
                line.startWidth = startWidth / f * scale;
                line.endWidth = endWidth / f * scale;
            }

            if (endPoint != null)
            {
                endPoint.position = end;
            }
        }

        public void SetVolumeColor()
        {
            SetColor(volumeColor);
        }

        public void SetWidgetColor()
        {
            SetColor(widgetColor);
        }

        public void SetPanelColor()
        {
            SetColor(panelColor);
        }

        public void SetHandleColor()
        {
            SetColor(handleColor);
        }

        public void SetColor(Gradient color)
        {
            line.colorGradient = color;
            Color endPointColor = color.Evaluate(endPointColorGrandientPct);
            endPoint.GetComponent<MeshRenderer>().material.SetColor("_UnlitColor", endPointColor);
        }
    }
}
