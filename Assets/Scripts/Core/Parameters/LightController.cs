using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LightController : ParametersController
    {
        private Light lightObject = null;

        public LightType lightType;
        public float intensity = 50f;
        public float minIntensity = 0f;
        public float maxIntensity = 100f;
        public Color color = Color.white;
        public bool castShadows = false;
        public float near = 0.01f;
        public float range = 20f;
        public float minRange = 0f;
        public float maxRange = 100f;
        public float outerAngle = 100f;
        public float innerAngle = 80f;

        public void SetLightEnable(bool enable)
        {
            if(lightObject)
                lightObject.gameObject.SetActive(enable);
        }

        public override void CopyParameters(ParametersController otherController)
        {
            base.CopyParameters(otherController);

            LightController other = otherController as LightController;
            lightType = other.lightType;
            intensity = other.intensity;
            minIntensity = other.minIntensity;
            maxIntensity = other.maxIntensity;
            color = other.color;
            castShadows = other.castShadows;
            near = other.near;
            range = other.range;
            minRange = other.minRange;
            maxRange = other.maxRange;
            outerAngle = other.outerAngle;
            innerAngle = other.innerAngle;
        }

        public void SetPower(float power)
        {
            switch (lightType)
            {
                case LightType.Point:
                    intensity = power * 0.1f;
                    break;
                case LightType.Directional:
                    intensity = power * 1.5f;
                    break;
                case LightType.Spot:
                    intensity = power * (0.4f / 3f);
                    break;
            }
        }

        public float GetPower()
        {
            switch (lightType)
            {
                case LightType.Point:
                    return intensity * 10f;
                case LightType.Directional:
                    return intensity / 1.5f;  
                case LightType.Spot:
                    return intensity / (0.4f / 3f);
            }
            return 0;
        }

        public void Init()
        {
            Light l = transform.GetComponentInChildren<Light>(true);
            lightType = l.type;
            lightObject = l;
            switch (lightType)
            {
                case LightType.Directional:
                    intensity = 10.0f;
                    minIntensity = 0.0f;
                    maxIntensity = 100.0f;
                    break;
                case LightType.Point:
                    intensity = 50.0f;
                    minIntensity = 0.0f;
                    maxIntensity = 100.0f;
                    range = 10f;
                    minRange = 0f;
                    maxRange = 100f;
                    break;
                case LightType.Spot:
                    intensity = 50.0f;
                    minIntensity = 0.0f;
                    maxIntensity = 100.0f;
                    near = 0.01f;
                    range = 20f;
                    minRange = 0f;
                    maxRange = 100f;
                    outerAngle = 100f;
                    innerAngle = 80;
                    break;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            Init();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (!lightObject)
                return;

            if (null == world)
                GetWorldTransform();


            float scale = GlobalState.WorldScale;
            if (lightObject.type == LightType.Directional)
                scale = 1f;            
            lightObject.intensity = (scale * scale * intensity);
            lightObject.range = scale * range;
            lightObject.shadowNearPlane = scale * near;
            lightObject.color = color;
            LightShadows shadows = GlobalState.Settings.castShadows && castShadows ? LightShadows.Soft : LightShadows.None;
            if (shadows != lightObject.shadows)
                lightObject.shadows = shadows;

            if (lightObject.type == LightType.Spot)
            {
                lightObject.spotAngle = outerAngle;
                lightObject.intensity *= 4f;
            }
            if (lightObject.type == LightType.Directional)
            {
                lightObject.intensity *= 0.05f;
            }
            // avoid flicking
            float invWorldScale = 1f / GlobalState.WorldScale;
            lightObject.transform.localScale = new Vector3(invWorldScale, invWorldScale, invWorldScale);
        }
    }

}