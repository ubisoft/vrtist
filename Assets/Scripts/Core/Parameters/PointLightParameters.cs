using Newtonsoft.Json;
using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class PointLightParameters : LightParameters
    {
        [JsonProperty("range")]
        public float range = 10f;
        [JsonProperty("minRange")]
        public float minRange = 0f;
        [JsonProperty("maxRange")]
        public float maxRange = 100f;

        public PointLightParameters()
        {
            lightType = LightType.Point;
            intensity = 10.0f;
            minIntensity = 0.0f;
            maxIntensity = 100.0f;
        }

        public override LightType GetLightType() { return LightType.Point; }
        protected override GameObject GetPrefab()
        {
            return Resources.Load("Prefabs/Point") as GameObject;
        }

        public override float GetRange() { return range; }
        public override float GetMinRange() { return minRange; }
        public override float GetMaxRange() { return maxRange; }

        public override void SetRange(float value) { range = value; }
        public override void SetMinRange(float value) { minRange = value; }
        public override void SetMaxRange(float value) { maxRange = value; }
    }
}