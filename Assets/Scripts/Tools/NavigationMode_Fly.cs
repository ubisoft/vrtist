using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{ 
    public class NavigationMode_Fly : NavigationMode
    {
        private float flySpeed = 0.2f;
        private bool rotating = false;

        public NavigationMode_Fly(float speed)
        {
            flySpeed = speed;
        }

        public override bool IsCompatibleWithPalette()
        {
            return true;
        }

        public override bool IsCompatibleWithUndoRedo()
        {
            return true;
        }

        public override bool IsCompatibleWithReset()
        { 
            return true;
        }

        public override void Init(Transform cameraTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform)
        {
            base.Init(cameraTransform, worldTransform, leftHandleTransform, pivotTransform);

            // Create tooltips
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Trigger, "Display Palette");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Primary, "Undo");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Secondary, "Redo");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Move / Turn");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Grip, "Grip World");
        }

        public override void DeInit()
        {
            base.DeInit();
        }

        public override void Update()
        {
            //
            // Joystick -- go forward/backward, and rotate 45 degrees.
            //

            Vector2 val = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            if (val != Vector2.zero)
            {
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                Vector3 velocity = Camera.main.transform.forward * val.y * d;
                camera.position += velocity * flySpeed;

                if (Mathf.Abs(val.x) > 0.95f && !rotating)
                {
                    camera.rotation *= Quaternion.Euler(0f, Mathf.Sign(val.x) * 45f, 0f);
                    rotating = true;
                }
                if (Mathf.Abs(val.x) <= 0.95f && rotating)
                {
                    rotating = false;
                }
            }

            //
            // LEFT GRIP WORLD
            //

            //VRInput.ButtonEvent(VRInput.leftController, CommonUsages.gripButton,
            //() =>
            //{
            //    // left AFTER right => reset all
            //    // NOTE: isRightGripped && Selection.selection.Count > 0 means that the selectionTool will/has gripped objects,
            //    //       and is no longer able to be used for two-hands interaction.
            //    if (isRightGripped && Selection.selection.Count == 0)
            //    {
            //        ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
            //        ResetInitWorldMatrix();
            //        ResetDistance(); // after reset world, use scale

            //        SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);

            //        lineUI.Show(true, StretchUI.LineMode.DOUBLE);
            //        GlobalState.isGrippingWorld = true;
            //    }
            //    else // only left => reset left
            //    {
            //        ResetInitControllerMatrices(ResetType.LEFT_ONLY);
            //        ResetInitWorldMatrix();

            //        SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL); // old hide

            //        lineUI.Show(true, StretchUI.LineMode.SINGLE);
            //        GlobalState.isGrippingWorld = false;
            //    }

            //    isLeftGripped = true;
            //},
            //() =>
            //{
            //    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);

            //    lineUI.Show(false);
            //    GlobalState.isGrippingWorld = false;

            //    isLeftGripped = false;
            //});
        }
    }
}
