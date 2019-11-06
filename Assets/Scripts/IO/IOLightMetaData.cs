using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class IOLightMetaData : IOMetaData
    {
        public enum LightType
        {
            Sun,
            Point,
            Spot
        }

        public LightType lightType;
    }
}
