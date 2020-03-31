using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Est-ce qu'il faudrait pas en faire une property, qui Invoke un event sur lequel le Selector/SelectorTrigger peut etre branche.
// Comme ca le grip de selection pourrait lacher au moment ou on GripWorld.
// GlobalState.isGrippingWorld = true; 
// GlobalState.GrippingWorld = true;  <----

namespace VRtist
{
    public class NavigationMode
    {
        protected Transform camera = null;
        protected Transform world = null;
        protected Transform leftHandle = null;
        protected Transform pivot = null;

        // Clip Planes config. Can be set back to PlayerController if we need tweaking.
        private bool useScaleFactor = false;
        private float nearPlaneFactor = 0.1f;
        private float farPlaneFactor = 5000.0f;
        private float nearPlane = 0.1f; // 10 cm, close enough to not clip the controllers.
        private float farPlane = 1000.0f; // 1km from us, far enough?

        protected enum ControllerVisibility { SHOW_NORMAL, HIDE, SHOW_GRIP };

        //
        // Virtual functions used for navigation by the PlayerController
        //
        public virtual bool IsCompatibleWithPalette() { return true; } // doesn't use the Trigger button
        public virtual bool IsCompatibleWithUndoRedo() { return true; } // doesn't use the Primary and Secondary buttons
        public virtual bool IsCompatibleWithReset() { return true; } // doesn't use the primary2DAxisClick

        public virtual void Init(Transform cameraTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform) 
        {
            camera = cameraTransform;
            world = worldTransform;
            leftHandle = leftHandleTransform;
            pivot = pivotTransform;

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
