using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class SunLightParameters : LightParameters
    {
        public override LightType GetType() { return LightType.Sun; }
        protected override GameObject GetPrefab()
        {
            return Resources.Load("Prefabs/Sun") as GameObject;
        }
    }
}