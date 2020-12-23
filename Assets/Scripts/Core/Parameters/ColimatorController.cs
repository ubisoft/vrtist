using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class ColimatorController : LocatorController
    {
        public bool isVRtist = false;
        public override bool IsDeletable()
        {
            return false;
        }
    }
}
