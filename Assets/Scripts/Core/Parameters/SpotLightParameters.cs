using System;
using UnityEngine;
using Newtonsoft.Json;

namespace VRtist
{
    [Serializable]
    public class SpotLightParameters : LightParameters
    {
        [JsonProperty("range")]
        public float range = 5f;
        [JsonProperty("minRange")]
        public float minRange = 0f;
        [JsonProperty("maxRange")]
        public float maxRange = 10f;

        public float near = 0.01f;
        public float outerAngle = 20f;
        public float innerAngle = 30f;

        public SpotLightParameters()
        {
            lightType = LightType.Spot;
            intensity = 3.0f;
            minIntensity = 0.0f;
            maxIntensity = 15.0f;
        }

        public override LightType GetLightType() { return LightType.Spot; }
        protected override GameObject GetPrefab()
        {
            return Resources.Load("Prefabs/Spot") as GameObject;
        }
        public override float GetRange() { return range; }
        public override float GetMinRange() { return minRange; }
        public override float GetMaxRange() { return maxRange; }

        public override void SetRange(float value) { range = value; }
        public override void SetMinRange(float value) { minRange = value; }
        public override void SetMaxRange(float value) { maxRange = value; }

        public override float GetNear() { return near; }
        public override void SetNear(float value) { near = value; }
        public override float GetInnerAngle() { return innerAngle; }
        public override void SetInnerAngle(float value) { innerAngle = value; }
        public override float GetOuterAngle() { return outerAngle; }
        public override void SetOuterAngle(float value) { outerAngle = value; }
    }
}
