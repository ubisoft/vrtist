using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GlobalState : MonoBehaviour
    {
        public static bool isGrippingWorld = false;
        public static float worldScale = 1f;

        public static int startFrame = 1;
        public static int endFrame = 250;
        public static int currentFrame = 1;

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }
    }
}
