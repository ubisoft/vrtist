using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LightingParameters : MonoBehaviour
    {
        public float intensity = 8f;
        public float range = 10f;
        public float near = 0.01f;
        public Color color = Color.white;

        public float outerAngle = 20f;
        public float innerAngle = 30f;
        public bool castShadows = false;

        private Transform world;
        private Light lightObject = null;

        public LightType LightType
        {
            get
            {
                return lightObject.type;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            world = transform.parent;
            while (world.parent)
            {
                world = world.parent;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                Light l = child.GetComponent<Light>();
                if (l != null)
                {
                    lightObject = l;
                    break;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!lightObject)
                return;

            float scale = world.localScale.x;
            if (lightObject.type == LightType.Directional)
                scale = 1f;
            lightObject.intensity = (scale * scale * intensity);
            lightObject.range = scale * range;
            lightObject.shadowNearPlane = scale * near;
            lightObject.color = color;
            lightObject.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
            if (lightObject.type == LightType.Spot)
            {
                lightObject.innerSpotAngle = innerAngle;
                lightObject.spotAngle = outerAngle;
            }
        }
    }

}