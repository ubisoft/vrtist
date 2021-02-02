using UnityEngine;

namespace VRtist
{
    public class LightController : ParametersController
    {
        public float minIntensity;
        public float maxIntensity;
        public float minRange;
        public float maxRange;

        private Light _lightObject = null;
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
                return LightObject.intensity;
            }
            set
            {
                LightObject.intensity = value;
            }
        }
        public Color Color { get { return LightObject.color; } set { LightObject.color = value; } }
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
                LightShadows shadows = GlobalState.Settings.castShadows && value ? LightShadows.Soft : LightShadows.None;
                if (shadows != LightObject.shadows)
                    LightObject.shadows = shadows;
            }
        }
        public float ShadowNearPlane { get { return LightObject.shadowNearPlane; } set { LightObject.shadowNearPlane = value; } }
        public float Range { get { return LightObject.range; } set { LightObject.range = value; } }
        public float OuterAngle { get { return LightObject.spotAngle; } set { LightObject.spotAngle = value; } }
        public float InnerAngle { get { return LightObject.innerSpotAngle; } set { LightObject.innerSpotAngle = value; } }

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
            InnerAngle = other.InnerAngle;
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
            if (_lightObject == null)
            {
                _lightObject = transform.GetComponentInChildren<Light>(true);

                // Init defaults
                _lightObject.shadowNearPlane = 0.01f;
                _lightObject.shadows = LightShadows.None;
                _lightObject.color = Color.white;
                switch (_lightObject.type)
                {
                    case LightType.Directional:
                        _lightObject.intensity = 10.0f;
                        minIntensity = 0.0f;
                        maxIntensity = 100.0f;
                        break;
                    case LightType.Point:
                        _lightObject.intensity = 0.5f;
                        minIntensity = 0.0f;
                        maxIntensity = 10.0f;
                        _lightObject.range = 10f;
                        minRange = 0f;
                        maxRange = 100f;
                        break;
                    case LightType.Spot:
                        _lightObject.intensity = 0.5f;
                        minIntensity = 0.0f;
                        maxIntensity = 10.0f;
                        _lightObject.shadowNearPlane = 0.01f;
                        _lightObject.range = 20f;
                        minRange = 0f;
                        maxRange = 100f;
                        _lightObject.spotAngle = 100f;
                        _lightObject.innerSpotAngle = 80;
                        break;
                }
            }
        }

        public override bool IsSnappable()
        {
            return false;
        }
    }
}
