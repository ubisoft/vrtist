using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{

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
            curves.Add(AnimatableProperty.LightIntensity, new Curve(AnimatableProperty.LightIntensity));
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
