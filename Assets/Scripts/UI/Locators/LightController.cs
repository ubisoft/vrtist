using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LightController : ParametersController
    {
        private Light lightObject = null;

        public LightType lightType;
        public float intensity = 8f;
        public float minIntensity = 0f;
        public float maxIntensity = 100f;
        public Color color = Color.white;
        public bool castShadows = false;
        public float near = 0.01f;
        public float range = 5f;
        public float minRange = 0f;
        public float maxRange = 100f;
        public float outerAngle = 20f;
        public float innerAngle = 30f;

        public void SetLightEnable(bool enable)
        {
            lightObject.gameObject.SetActive(enable);
        }

        public void CopyParameters(LightController other)
        {
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

        public void Init()
        {
            Light l = transform.GetComponentInChildren<Light>();
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
                    intensity = 10.0f;
                    minIntensity = 0.0f;
                    maxIntensity = 100.0f;
                    range = 10f;
                    minRange = 0f;
                    maxRange = 100f;
                    break;
                case LightType.Spot:
                    intensity = 3.0f;
                    minIntensity = 0.0f;
                    maxIntensity = 100.0f;
                    near = 0.01f;
                    range = 5f;
                    minRange = 0f;
                    maxRange = 100f;
                    outerAngle = 20f;
                    innerAngle = 30f;
                    break;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            Init();
        }

        // Update is called once per frame
        void Update()
        {
            if (!lightObject)
                return;

            if (null == world)
                GetWorldTransform();

            float scale = world.localScale.x;
            if (lightObject.type == LightType.Directional)
                scale = 1f;
            lightObject.intensity = (scale * scale * intensity);
            lightObject.range = scale * range;
            lightObject.shadowNearPlane = scale * near;
            lightObject.color = color;
            LightShadows shadows = GlobalState.castShadows && castShadows ? LightShadows.Soft : LightShadows.None;
            if (shadows != lightObject.shadows)
                lightObject.shadows = shadows;
            if (lightObject.type == LightType.Spot)
            {
                lightObject.innerSpotAngle = innerAngle;
                lightObject.spotAngle = outerAngle;
            }

            // avoid flicking
            lightObject.transform.localScale = new Vector3(1f / world.localScale.x, 1f / world.localScale.x, 1f / world.localScale.x);            
        }
    }

}