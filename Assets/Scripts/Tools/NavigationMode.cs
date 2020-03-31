using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class NavigationMode
    {
        protected Transform rig = null;
        protected Transform world = null;
        protected Transform leftHandle = null;
        protected Transform pivot = null;
        protected Transform camera = null;

        public UsedControls usedControls = UsedControls.NONE;

        // Clip Planes config. Can be set back to PlayerController if we need tweaking.
        private bool useScaleFactor = false;
        private float nearPlaneFactor = 0.1f;
        private float farPlaneFactor = 5000.0f;
        private float nearPlane = 0.1f; // 10 cm, close enough to not clip the controllers.
        private float farPlane = 1000.0f; // 1km from us, far enough?

        protected enum ControllerVisibility { SHOW_NORMAL, HIDE, SHOW_GRIP };

        [System.Flags]
        public enum UsedControls 
        {
            NONE                 = (1 << 0),

            LEFT_JOYSTICK        = (1 << 1),
            LEFT_JOYSTICK_CLICK  = (1 << 2),
            LEFT_TRIGGER         = (1 << 3),
            LEFT_GRIP            = (1 << 4),
            LEFT_PRIMARY         = (1 << 5), 
            LEFT_SECONDARY       = (1 << 6),

            RIGHT_JOYSTICK       = (1 << 7),
            RIGHT_JOYSTICK_CLICK = (1 << 8),
            RIGHT_TRIGGER        = (1 << 9),
            RIGHT_GRIP           = (1 << 10),
            RIGHT_PRIMARY        = (1 << 11),
            RIGHT_SECONDARY      = (1 << 12)
        }

        public static bool HasFlag(UsedControls a, UsedControls b)
        {
            return (a & b) == b;
        }

        //
        // Virtual functions used for navigation by the PlayerController
        //

        // Pass only rig and world and Find("") the other nodes?
        public virtual void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform, Transform cameraTransform) 
        {
            rig = rigTransform;
            world = worldTransform;
            leftHandle = leftHandleTransform;
            pivot = pivotTransform;
            camera = cameraTransform;

            UpdateCameraClipPlanes();
        }

        public virtual void DeInit() { }

        public virtual void Update() { }

        //
        // Common Utils
        //
        protected void UpdateCameraClipPlanes()
        {
            if (useScaleFactor)
            {
                Camera.main.nearClipPlane = nearPlaneFactor * world.localScale.x; // 0.1f
                Camera.main.farClipPlane = farPlaneFactor * world.localScale.x; // 5000.0f
            }
            else
            {
                Camera.main.nearClipPlane = nearPlane;
                Camera.main.farClipPlane = farPlane;
            }
        }

        protected void SetLeftControllerVisibility(ControllerVisibility visibility)
        {
            leftHandle.localScale = visibility == ControllerVisibility.HIDE ? Vector3.zero : Vector3.one;
        }
    }
}
