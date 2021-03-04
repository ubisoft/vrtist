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
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    public class LightController : ParametersController
    {
        public float minIntensity;
        public float maxIntensity;
        public float minRange;
        public float maxRange;

        private Light _lightObject = null;
        private HDAdditionalLightData lightData = null;
        private HDAdditionalLightData LightData
        {
            get
            {
                if (null == lightData)
                {
                    Init();
                }
                return lightData;
            }
        }
        private Light LightObject
        {
            get
            {
                Init();
                return _lightObject;
            }
        }

        public LightType Type { get { return LightObject.type; } set { LightObject.type = value; } }
        public float Intensity
        {
            get
            {
                return LightData.intensity;
            }
            set
            {
                LightData.SetIntensity(value);
            }
        }
        public Color Color
        {
            get
            {
                return LightData.color;
            }
            set
            {
                LightData.color = value;
            }
        }
        private bool _castShadows = false;
        public bool CastShadows
        {
            get
            {
                return _castShadows;
            }
            set
            {
                _castShadows = value;
                LightData.EnableShadows(value);
            }
        }
        public float ShadowNearPlane { get { return LightData.shadowNearPlane; } set { LightData.shadowNearPlane = value; } }
        public float Range { get { return LightData.range; } set { LightData.SetRange(value); } }

        private float _outerAngle = 100f;
        private float _sharpness = 80f;
        public float OuterAngle { get { return _outerAngle; } set { _outerAngle = value; LightData.SetSpotAngle(value, _sharpness); } }
        public float Sharpness { get { return _sharpness; } set { _sharpness = value; LightData.SetSpotAngle(_outerAngle, value); } }

        public void SetLightEnable(bool enable)
        {
            LightObject.gameObject.SetActive(enable);
        }

        public override void CopyParameters(ParametersController otherController)
        {
            base.CopyParameters(otherController);

            LightController other = otherController as LightController;

            Intensity = other.Intensity;
            minIntensity = other.minIntensity;
            maxIntensity = other.maxIntensity;
            Color = other.Color;
            CastShadows = other.CastShadows;
            ShadowNearPlane = other.ShadowNearPlane;
            Range = other.Range;
            minRange = other.minRange;
            maxRange = other.maxRange;
            OuterAngle = other.OuterAngle;
            Sharpness = other.Sharpness;
        }

        public void OnCastShadowsChanged(bool _)
        {
            // Binding set in the lightBuilder
            CastShadows = _castShadows;
        }

        public void SetPower(float power)
        {
            switch (Type)
            {
                case LightType.Point:
                    Intensity = power * 0.1f;
                    break;
                case LightType.Directional:
                    Intensity = power * 1.5f;
                    break;
                case LightType.Spot:
                    Intensity = power * (0.4f / 3f);
                    break;
            }
        }

        public float GetPower()
        {
            switch (Type)
            {
                case LightType.Point:
                    return Intensity * 10f;
                case LightType.Directional:
                    return Intensity / 1.5f;
                case LightType.Spot:
                    return Intensity / (0.4f / 3f);
                case LightType.Area:
                    break;
                case LightType.Disc:
                    break;
            }
            return 0;
        }

        private void Init()
        {
            if (lightData == null)
            {
                _lightObject = transform.GetComponentInChildren<Light>(true);
                lightData = transform.GetComponentInChildren<HDAdditionalLightData>(true);

                // Init defaults
                lightData.shadowNearPlane = 0.01f;
                CastShadows = false;
                Color = Color.white;
                switch (_lightObject.type)
                {
                    case LightType.Directional:
                        Intensity = 5f;
                        minIntensity = 0.0f;
                        maxIntensity = 10.0f;
                        break;
                    case LightType.Point:
                        Intensity = 5f;
                        minIntensity = 0.0f;
                        maxIntensity = 10.0f;
                        Range = 10f;
                        minRange = 0f;
                        maxRange = 100f;
                        break;
                    case LightType.Spot:
                        Intensity = 5f;
                        minIntensity = 0.0f;
                        maxIntensity = 10.0f;
                        Range = 2f;
                        minRange = 0f;
                        maxRange = 100f;
                        Sharpness = 80f;
                        OuterAngle = 100f;
                        break;
                }
            }
        }

        public override bool IsSnappable()
        {
            return false;
        }
        public override bool IsDeformable()
        {
            return false;
        }
    }
}
