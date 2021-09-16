/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
 * Université de Rennes 1 / Invictus Project
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
    /// A set of animations for a given Transform. An animation is a curve on specific properties (position, rotation...).
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

        public AnimationSet(AnimationSet set)
        {
            transform = set.transform;
            foreach (KeyValuePair<AnimatableProperty, Curve> curve in set.curves)
            {
                curves.Add(curve.Key, new Curve(curve.Key));
                SetCurve(curve.Key, curve.Value.keys);
                curves[curve.Key].ComputeCache();
            }
        }

        public void EvaluateAnimation(int currentFrame)
        {
            Vector3 position = transform.localPosition;
            Vector3 rotation = transform.localEulerAngles;
            Vector3 scale = transform.localScale;

            float power = -1;
            Color color = Color.white;

            float cameraFocal = -1;
            float cameraFocus = -1;
            float cameraAperture = -1;

            foreach (Curve curve in curves.Values)
            {
                if (!curve.Evaluate(currentFrame, out float value))
                    continue;
                switch (curve.property)
                {
                    case AnimatableProperty.PositionX: position.x = value; break;
                    case AnimatableProperty.PositionY: position.y = value; break;
                    case AnimatableProperty.PositionZ: position.z = value; break;

                    case AnimatableProperty.RotationX: rotation.x = value; break;
                    case AnimatableProperty.RotationY: rotation.y = value; break;
                    case AnimatableProperty.RotationZ: rotation.z = value; break;

                    case AnimatableProperty.ScaleX: scale.x = value; break;
                    case AnimatableProperty.ScaleY: scale.y = value; break;
                    case AnimatableProperty.ScaleZ: scale.z = value; break;

                    case AnimatableProperty.Power: power = value; break;
                    case AnimatableProperty.ColorR: color.r = value; break;
                    case AnimatableProperty.ColorG: color.g = value; break;
                    case AnimatableProperty.ColorB: color.b = value; break;

                    case AnimatableProperty.CameraFocal: cameraFocal = value; break;
                    case AnimatableProperty.CameraFocus: cameraFocus = value; break;
                    case AnimatableProperty.CameraAperture: cameraAperture = value; break;
                }
            }

            transform.localPosition = position;
            transform.localEulerAngles = rotation;
            transform.localScale = scale;

            if (power != -1)
            {
                LightController controller = transform.GetComponent<LightController>();
                controller.Power = power;
                controller.Color = color;
            }

            if (cameraFocal != -1 || cameraFocus != -1 || cameraAperture != -1)
            {
                CameraController controller = transform.GetComponent<CameraController>();
                if (cameraFocal != -1)
                    controller.focal = cameraFocal;
                if (cameraFocus != -1)
                    controller.Focus = cameraFocus;
                if (cameraAperture != -1)
                    controller.aperture = cameraAperture;
            }
        }

        /// <summary>
        /// Apply animation on an other Transform
        /// </summary>
        public void EvaluateTransform(int currentFrame, Transform target)
        {
            Vector3 position = target.localPosition;
            Vector3 rotation = target.localEulerAngles;
            Vector3 scale = target.localScale;

            foreach (Curve curve in curves.Values)
            {
                if (!curve.Evaluate(currentFrame, out float value))
                    continue;
                switch (curve.property)
                {
                    case AnimatableProperty.PositionX: position.x = value; break;
                    case AnimatableProperty.PositionY: position.y = value; break;
                    case AnimatableProperty.PositionZ: position.z = value; break;

                    case AnimatableProperty.RotationX: rotation.x = value; break;
                    case AnimatableProperty.RotationY: rotation.y = value; break;
                    case AnimatableProperty.RotationZ: rotation.z = value; break;

                    case AnimatableProperty.ScaleX: scale.x = value; break;
                    case AnimatableProperty.ScaleY: scale.y = value; break;
                    case AnimatableProperty.ScaleZ: scale.z = value; break;
                }
            }

            target.localPosition = position;
            target.localEulerAngles = rotation;
            target.localScale = scale;
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
            curves.Add(AnimatableProperty.CameraFocus, new Curve(AnimatableProperty.CameraFocus));
            curves.Add(AnimatableProperty.CameraAperture, new Curve(AnimatableProperty.CameraAperture));
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

        public void SetStartOffset(int offset)
        {
            foreach (KeyValuePair<AnimatableProperty, Curve> pair in curves)
            {
                for (int i = 0; i < pair.Value.keys.Count; i++)
                {
                    pair.Value.keys[i].frame += offset;
                }
                pair.Value.ComputeCache();
            }
        }

        public int GetFirstFrame()
        {
            int firstFrame = GlobalState.Animation.EndFrame;
            foreach (KeyValuePair<AnimatableProperty, Curve> pair in curves)
            {
                if (pair.Value.keys.Count > 0)
                {
                    int curveFirstFrame = pair.Value.keys[0].frame;
                    if (curveFirstFrame < firstFrame) firstFrame = curveFirstFrame;
                }
            }
            return firstFrame;
        }

        public Matrix4x4 GetTRSMatrix(int frame)
        {
            Vector3 position = Vector3.zero;
            Curve posx = GetCurve(AnimatableProperty.PositionX);
            Curve posy = GetCurve(AnimatableProperty.PositionY);
            Curve posz = GetCurve(AnimatableProperty.PositionZ);
            if (null != posx && null != posy && null != posz)
            {
                if (!posx.Evaluate(frame, out float px) || !posy.Evaluate(frame, out float py) || !posz.Evaluate(frame, out float pz))
                {
                    px = transform.localPosition.x;
                    py = transform.localPosition.y;
                    pz = transform.localPosition.z;
                }
                position = new Vector3(px, py, pz);
            }
            Quaternion rotation = Quaternion.identity;
            Curve rotx = GetCurve(AnimatableProperty.RotationX);
            Curve roty = GetCurve(AnimatableProperty.RotationY);
            Curve rotz = GetCurve(AnimatableProperty.RotationZ);
            if (null != posx && null != roty && null != rotz)
            {
                if (!rotx.Evaluate(frame, out float rx) || !roty.Evaluate(frame, out float ry) || !rotz.Evaluate(frame, out float rz))
                {
                    rx = transform.localEulerAngles.x;
                    ry = transform.localEulerAngles.y;
                    rz = transform.localEulerAngles.z;
                }
                rotation = Quaternion.Euler(rx, ry, rz);
            }
            Vector3 scale = Vector3.one;
            Curve scalex = GetCurve(AnimatableProperty.ScaleX);
            Curve scaley = GetCurve(AnimatableProperty.ScaleY);
            Curve scalez = GetCurve(AnimatableProperty.ScaleZ);
            if (null != scalex && null != scaley && null != scalez)
            {
                if (scalex.Evaluate(frame, out float sx) && scaley.Evaluate(frame, out float sy) && scalez.Evaluate(frame, out float sz))
                {
                    scale = new Vector3(sx, sy, sz);
                }
            }
            return Matrix4x4.TRS(position, rotation, scale);
        }
    }

}
