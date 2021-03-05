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

using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// A set of animations for a given Transform. An animation is a curve on specific properties (location, rotation...).
    /// </summary>
    public class AnimationSet
    {
        public Transform transform;
        public readonly Dictionary<AnimatableProperty, Curve> curves = new Dictionary<AnimatableProperty, Curve>();

        public AnimationSet(GameObject gobject)
        {
            transform = gobject.transform;
            LightController lightController = gobject.GetComponent<LightController>();
            CameraController cameraController = gobject.GetComponent<CameraController>();
            if (null != lightController) { CreateLightCurves(); }
            else if (null != cameraController) { CreateCameraCurves(); }
            else { CreateTransformCurves(); }
        }

        public Curve GetCurve(AnimatableProperty property)
        {
            curves.TryGetValue(property, out Curve result);
            return result;
        }

        public void SetCurve(AnimatableProperty property, List<AnimationKey> keys)
        {
            if (!curves.TryGetValue(property, out Curve curve))
            {
                Debug.LogError("Curve not found : " + transform.name + " " + property.ToString());
                return;
            }
            curve.SetKeys(keys);
        }

        private void CreatePositionRotationCurves()
        {
            curves.Add(AnimatableProperty.PositionX, new Curve(AnimatableProperty.PositionX));
            curves.Add(AnimatableProperty.PositionY, new Curve(AnimatableProperty.PositionY));
            curves.Add(AnimatableProperty.PositionZ, new Curve(AnimatableProperty.PositionZ));

            curves.Add(AnimatableProperty.RotationX, new Curve(AnimatableProperty.RotationX));
            curves.Add(AnimatableProperty.RotationY, new Curve(AnimatableProperty.RotationY));
            curves.Add(AnimatableProperty.RotationZ, new Curve(AnimatableProperty.RotationZ));
        }

        private void CreateTransformCurves()
        {
            CreatePositionRotationCurves();
            curves.Add(AnimatableProperty.ScaleX, new Curve(AnimatableProperty.ScaleX));
            curves.Add(AnimatableProperty.ScaleY, new Curve(AnimatableProperty.ScaleY));
            curves.Add(AnimatableProperty.ScaleZ, new Curve(AnimatableProperty.ScaleZ));
        }

        private void CreateLightCurves()
        {
            CreatePositionRotationCurves();
            curves.Add(AnimatableProperty.Power, new Curve(AnimatableProperty.Power));
            curves.Add(AnimatableProperty.ColorR, new Curve(AnimatableProperty.ColorR));
            curves.Add(AnimatableProperty.ColorG, new Curve(AnimatableProperty.ColorG));
            curves.Add(AnimatableProperty.ColorB, new Curve(AnimatableProperty.ColorB));
        }

        private void CreateCameraCurves()
        {
            CreatePositionRotationCurves();
            curves.Add(AnimatableProperty.CameraFocal, new Curve(AnimatableProperty.CameraFocal));
        }

        public void ComputeCache()
        {
            foreach (Curve curve in curves.Values)
                curve.ComputeCache();
        }

        public void ClearCache()
        {
            foreach (Curve curve in curves.Values)
                curve.ClearCache();
        }
    }

}
