using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class SpotLightParameters : LightParameters
    {
        public float range = 10f;
        public float near = 0.01f;
        public float outerAngle = 20f;
        public float innerAngle = 30f;

        public override LightType GetType() { return LightType.Spot; }
        protected override GameObject GetPrefab()
        {
            return Resources.Load("Prefabs/Spot") as GameObject;
        }
        public override float GetRange() { return range; }
        public override void SetRange(float value) { range = value; }
        public override float GetNear() { return near; }
        public override void SetNear(float value) { near = value; }
        public override float GetInnerAngle() { return innerAngle; }
        public override void SetInnerAngle(float value) { innerAngle = value; }
        public override float GetOuterAngle() { return outerAngle; }
        public override void SetOuterAngle(float value) { outerAngle = value; }

    }
}