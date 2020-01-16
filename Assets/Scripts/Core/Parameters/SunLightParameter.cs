using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class SunLightParameters : LightParameters
    {
        public SunLightParameters()
        {
            lightType = LightType.Sun;
            intensity = 10.0f;
            minIntensity = 0.0f;
            maxIntensity = 100.0f;
        }

        public override LightType GetLightType() { return LightType.Sun; }
        protected override GameObject GetPrefab()
        {
            return Resources.Load("Prefabs/Sun") as GameObject;
        }
    }
}