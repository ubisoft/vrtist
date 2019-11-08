using Newtonsoft.Json;
using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class LightParameters : Parameters
    {
        public enum LightType
        {
            Sun,
            Point,
            Spot,
            Unknown
        }

        [JsonProperty("lightType")]
        public LightType lightType;
        [JsonProperty("intensity")]
        public float intensity = 8f;
        [JsonProperty("color")]
        public Color color = Color.white;
        [JsonProperty("castShadows")]
        public bool castShadows = false;

        protected virtual GameObject GetPrefab() { return null;  }
        public virtual LightType GetType() { return LightType.Unknown;  }

        public virtual float GetRange() { return 1f; }
        public virtual float GetNear() { return 0.01f; }
        public virtual float GetInnerAngle() { return 30f; }
        public virtual float GetOuterAngle() { return 90f; }

        public virtual void SetRange(float value) {  }
        public virtual void SetNear(float value) {  }
        public virtual void SetInnerAngle(float value) {  }
        public virtual void SetOuterAngle(float value) {  }

        public virtual Transform Deserialize(Transform parent)
        {
            GameObject light;
            GameObject lightPrefab = GetPrefab();

            light = Utils.CreateInstance(lightPrefab, parent);
            light.gameObject.GetComponent<LightController>().parameters = this;

            return light.transform;
        }
    }
}