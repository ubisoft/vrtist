using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LightController : ParametersController
    {
        public LightParameters parameters = null;
        public override Parameters GetParameters() { return parameters; }
        public void SetParameters(LightParameters p) { parameters = p; }

        private Light lightObject = null;

        public void SetLightEnable(bool enable)
        {
            lightObject.gameObject.SetActive(enable);
        }

        public void Init()
        {
            Light l = transform.GetComponentInChildren<Light>();
            LightType ltype = l.type;
            lightObject = l;
            switch (ltype)
            {
                case LightType.Directional:
                    parameters = new SunLightParameters();
                    break;
                case LightType.Point:
                    parameters = new PointLightParameters();
                    break;
                case LightType.Spot:
                    parameters = new SpotLightParameters();
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
            lightObject.intensity = (scale * scale * parameters.intensity);
            lightObject.range = scale * parameters.GetRange();
            lightObject.shadowNearPlane = scale * parameters.GetNear();
            lightObject.color = parameters.color;
            lightObject.shadows = parameters.castShadows ? LightShadows.Soft : LightShadows.None;
            if (lightObject.type == LightType.Spot)
            {
                lightObject.innerSpotAngle = parameters.GetInnerAngle();
                lightObject.spotAngle = parameters.GetOuterAngle();
            }

            // avoid flicking
            lightObject.transform.localScale = new Vector3(1f / world.localScale.x, 1f / world.localScale.x, 1f / world.localScale.x);            
        }
    }

}