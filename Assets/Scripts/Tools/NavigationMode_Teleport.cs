using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{ 
    public class NavigationMode_Teleport : NavigationMode
    {
        //private TeleportArc arc = null;

        private bool isLocked = false;

        private Vector3 targetPosition = Vector3.zero;

        private const float deadZone = 0.5f;

        public NavigationMode_Teleport(/*TeleportArc theArc*/)
        {
            //arc = theArc;
        }

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, pivotTransform, cameraTransform, parametersTransform);

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Target/Turn");

            usedControls = UsedControls.LEFT_JOYSTICK;

            //if (arc != null)
            //{
            //    arc.gameObject.SetActive(true);
            //    arc.SetDefaultColor();
            //}
        }

        public override void DeInit()
        {
            base.DeInit();

            //if (arc != null)
            //{
            //    arc.gameObject.SetActive(false);
            //}
        }

        public override void Update()
        {
            //if (arc == null)
            //    return;

        }
    }
}
