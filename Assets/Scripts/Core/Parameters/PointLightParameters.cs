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

        public override LightType GetType() { return LightType.Point; }
        protected override GameObject GetPrefab()
        {
            return Resources.Load("Prefabs/Point") as GameObject;
        }

        public override float GetRange() { return range; }
        public override void SetRange(float value) { range = value; }

    }
}