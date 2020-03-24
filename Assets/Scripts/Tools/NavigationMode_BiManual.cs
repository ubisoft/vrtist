using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{ 
    public class NavigationMode_BiManual : NavigationMode
    {
        private StretchUI lineUI = null;
        private float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        private float minPlayerScale = 50.0f; // world scale = 50.0f;

        private Matrix4x4 initLeftControllerMatrix_WtoL;
        private Matrix4x4 initRightControllerMatrix_WtoL;
        private Matrix4x4 initMiddleMatrix_WtoL;

        private Matrix4x4 initWorldMatrix_W;

        private bool isLeftGripped = false;
        private bool isRightGripped = false;

        private float prevDistance = 0.0f;
        private float scale;

        private const float deadZone = 0.3f;
        private const float fixedScaleFactor = 1.05f; // for grip world scale

        enum ResetType { LEFT_ONLY, LEFT_AND_RIGHT };

        public NavigationMode_BiManual(StretchUI line, float minScale, float maxScale)
        {
            if (lineUI == null) 
            { 
                Debug.LogWarning("Cannot find the stretch ui object"); 
            }

            lineUI = line;

            minPlayerScale = minScale;
            maxPlayerScale = maxScale;
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

            lineUI.Show(false);


            // Create tooltips
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Trigger, "Display Palette");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Primary, "Undo");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Secondary, "Redo");
            ////Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Move / Turn");
            //Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Grip, "Grip World");
        }

        public override void DeInit()
        {
            base.DeInit();
        }

        public override void Update()
        {
            //
            // LEFT GRIP WORLD
            //

            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.gripButton,
            () =>
            {
                // left AFTER right => reset all
                // NOTE: isRightGripped && Selection.selection.Count > 0 means that the selectionTool will/has gripped objects,
                //       and is no longer able to be used for two-hands interaction.
                if (isRightGripped && Selection.selection.Count == 0)
                {
                    ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
                    ResetInitWorldMatrix();
                    ResetDistance(); // after reset world, use scale

                    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);

                    lineUI.Show(true, StretchUI.LineMode.DOUBLE);
                    GlobalState.isGrippingWorld = true;
                }
                else // only left => reset left
                {
                    ResetInitControllerMatrices(ResetType.LEFT_ONLY);
                    ResetInitWorldMatrix();

                    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL); // old hide

                    lineUI.Show(true, StretchUI.LineMode.SINGLE);
                    GlobalState.isGrippingWorld = false;
                }

                isLeftGripped = true;
            },
            () =>
            {
                SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);

                lineUI.Show(false);
                GlobalState.isGrippingWorld = false;

                isLeftGripped = false;
            });

            //
            // RIGHT GRIP WORLD
            //

            // TODO: 
            //  * on a un souci d'ordre de recuperation d'evenement.
            //    on ne peut pas garantir que le selection tool va essayer de grip avant ou apres le player controller.
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton,
            () =>
            {
                if (Selection.selection.Count == 0)
                {
                    // right AFTER left and no selection, reset all
                    if (isLeftGripped)
                    {
                        ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
                        ResetInitWorldMatrix();
                        ResetDistance(); // NOTE: called after "reset world", because it uses the scale.

                        SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
                        lineUI.Show(true, StretchUI.LineMode.DOUBLE);
                        GlobalState.isGrippingWorld = true;
                    }

                    // even if no left gripped, just flag the right as gripped for the next update
                    isRightGripped = true;
                }
            },
            () =>
            {
                // si on relache le right et que le left est tjs grip, reset left
                if (isLeftGripped)
                {
                    ResetInitControllerMatrices(ResetType.LEFT_ONLY);
                    ResetInitWorldMatrix();

                    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL); // old hide

                    //lineUI.Show(false);
                    lineUI.Show(true, StretchUI.LineMode.SINGLE);
                    GlobalState.isGrippingWorld = false;
                }

                isRightGripped = false;
            });

            // NOTE: we test isLeftGrip because we can be ungripped but still over the deadzone, strangely.
            if (isLeftGripped && VRInput.GetValue(VRInput.leftController, CommonUsages.grip) > deadZone)
            {
                float prevScale = scale;

                // Scale using left joystick.
                if (!isRightGripped)
                {
                    Vector2 joystickAxis = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
                    if (joystickAxis.y > deadZone)
                        scale *= fixedScaleFactor;
                    if (joystickAxis.y < -deadZone)
                        scale /= fixedScaleFactor;

                    GlobalState.worldScale = scale;

                    // TODO: draw scale factor.
                }

                // update left joystick
                Vector3 currentLeftControllerPosition_L;
                Quaternion currentLeftControllerRotation_L;
                VRInput.GetControllerTransform(VRInput.leftController, out currentLeftControllerPosition_L, out currentLeftControllerRotation_L);
                Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));
                Matrix4x4 currentLeftControllerMatrix_W = pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled;
                Vector3 currentLeftControllerPosition_W = currentLeftControllerMatrix_W.MultiplyPoint(Vector3.zero);

                if (isRightGripped)
                {
                    // update right joystick
                    Vector3 currentRightControllerPosition_L;
                    Quaternion currentRightControllerRotation_L;
                    VRInput.GetControllerTransform(VRInput.rightController, out currentRightControllerPosition_L, out currentRightControllerRotation_L);
                    Matrix4x4 currentRightControllerMatrix_L_Scaled = Matrix4x4.TRS(currentRightControllerPosition_L, currentRightControllerRotation_L, new Vector3(scale, scale, scale));
                    Vector3 currentRightControllerPosition_W = (pivot.localToWorldMatrix * currentRightControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

                    Vector3 currentMiddleControllerPosition_W = (currentLeftControllerPosition_W + currentRightControllerPosition_W) * 0.5f;

                    // scale handling (before computing the "transformed" matrix with the new scale)
                    float newDistance = Vector3.Distance(currentLeftControllerPosition_W, currentRightControllerPosition_W);
                    float factor = newDistance / prevDistance;
                    float oldScale = scale;
                    scale *= factor;
                    prevDistance = newDistance;

                    Vector3 middlePosition_L = (currentLeftControllerPosition_L + currentRightControllerPosition_L) * 0.5f;
                    Vector3 middleXVector = (currentRightControllerPosition_L - currentLeftControllerPosition_L).normalized;
                    Vector3 middleForwardVector = -Vector3.Cross(middleXVector, pivot.up).normalized;
                    Quaternion middleRotation_L = Quaternion.LookRotation(middleForwardVector, pivot.up);

                    Matrix4x4 middleMatrix_L_OldScaled = Matrix4x4.TRS(middlePosition_L, middleRotation_L, new Vector3(oldScale, oldScale, oldScale));
                    Matrix4x4 middleMatrix_L_Scaled = Matrix4x4.TRS(middlePosition_L, middleRotation_L, new Vector3(scale, scale, scale));

                    Matrix4x4 middleMatrix_W_OldDelta = pivot.localToWorldMatrix * middleMatrix_L_OldScaled * initMiddleMatrix_WtoL;
                    Matrix4x4 middleMatrix_W_Delta = pivot.localToWorldMatrix * middleMatrix_L_Scaled * initMiddleMatrix_WtoL;

                    Matrix4x4 transformedOld = middleMatrix_W_OldDelta * initWorldMatrix_W;
                    Matrix4x4 transformed = middleMatrix_W_Delta * initWorldMatrix_W;

                    float s = 1.0f;
                    float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 1.0f / maxPlayerScale, minPlayerScale);
                    if (transformed.lossyScale.x != clampedScale)
                    {
                        world.localPosition = new Vector3(transformedOld.GetColumn(3).x, transformedOld.GetColumn(3).y, transformedOld.GetColumn(3).z);
                        world.localRotation = transformedOld.rotation;

                        s = scale;

                        scale = prevScale;
                    }
                    else
                    {
                        // translate/rotate/scale using the new scale
                        world.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                        world.localRotation = transformed.rotation;
                        world.localScale = new Vector3(clampedScale, clampedScale, clampedScale);

                        s = oldScale;
                    }

                    GlobalState.worldScale = s;

                    // Rotation for the line text
                    Vector3 middleForward180 = Vector3.Cross(middleXVector, pivot.up).normalized;
                    Vector3 rolledUp = Vector3.Cross(-middleXVector, middleForward180).normalized;
                    Quaternion middleRotationWithRoll_L = Quaternion.LookRotation(middleForward180, rolledUp);
                    Matrix4x4 middleMatrixWithRoll_L_Scaled = Matrix4x4.TRS(middlePosition_L, middleRotationWithRoll_L, new Vector3(s, s, s));
                    Quaternion middleRotationWithRoll_W = (pivot.localToWorldMatrix * middleMatrixWithRoll_L_Scaled).rotation;
                    lineUI.UpdateLineUI(currentLeftControllerPosition_W, currentRightControllerPosition_W, middleRotationWithRoll_W, world.localScale.x);
                }
                else
                {
                    Matrix4x4 currentLeftControllerMatrix_W_Delta = pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled * initLeftControllerMatrix_WtoL;
                    Matrix4x4 transformed = currentLeftControllerMatrix_W_Delta * initWorldMatrix_W;

                    world.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                    world.localRotation = transformed.rotation;
                    float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 1.0f / maxPlayerScale, minPlayerScale);
                    world.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
                    if (transformed.lossyScale.x != clampedScale)
                    {
                        scale = prevScale;
                    }

                    lineUI.UpdateLineUI(currentLeftControllerPosition_W, currentLeftControllerPosition_W, currentLeftControllerMatrix_W.rotation, world.localScale.x);
                }
                GlobalState.worldScale = world.localScale.x;

                UpdateCameraClipPlanes();
            }
        }

        private void ResetInitControllerMatrices(ResetType res)
        {
            Vector3 initLeftControllerPosition_L;
            Quaternion initLeftControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.leftController, out initLeftControllerPosition_L, out initLeftControllerRotation_L);
            Matrix4x4 initLeftControllerMatrix_L = Matrix4x4.TRS(initLeftControllerPosition_L, initLeftControllerRotation_L, Vector3.one);
            initLeftControllerMatrix_WtoL = (pivot.localToWorldMatrix * initLeftControllerMatrix_L).inverse;

            if (res == ResetType.LEFT_AND_RIGHT)
            {
                Vector3 initRightControllerPosition_L; // initial right controller position in local space.
                Quaternion initRightControllerRotation_L; // initial right controller rotation in local space.
                VRInput.GetControllerTransform(VRInput.rightController, out initRightControllerPosition_L, out initRightControllerRotation_L);
                Matrix4x4 initRightControllerMatrix_L = Matrix4x4.TRS(initRightControllerPosition_L, initRightControllerRotation_L, Vector3.one);
                initRightControllerMatrix_WtoL = (pivot.localToWorldMatrix * initRightControllerMatrix_L).inverse;

                Vector3 initMiddlePosition_L = (initLeftControllerPosition_L + initRightControllerPosition_L) * 0.5f;
                Vector3 middleXVector = (initRightControllerPosition_L - initLeftControllerPosition_L).normalized;
                Vector3 middleForwardVector = -Vector3.Cross(middleXVector, pivot.up).normalized;
                Quaternion initMiddleRotation_L = Quaternion.LookRotation(middleForwardVector, pivot.up);
                Matrix4x4 initMiddleMatrix_L = Matrix4x4.TRS(initMiddlePosition_L, initMiddleRotation_L, Vector3.one);
                initMiddleMatrix_WtoL = (pivot.localToWorldMatrix * initMiddleMatrix_L).inverse;
            }
        }

        private void ResetInitWorldMatrix()
        {
            initWorldMatrix_W = world.localToWorldMatrix;
            scale = 1f;
            GlobalState.worldScale = scale;
        }

        private void ResetDistance()
        {
            // compute left controller world space position
            Vector3 currentLeftControllerPosition_L;
            Quaternion currentLeftControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.leftController, out currentLeftControllerPosition_L, out currentLeftControllerRotation_L);
            Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));
            Vector3 currentLeftControllerPosition_W = (pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

            // compute right controller world space position
            Vector3 currentRightControllerPosition_L;
            Quaternion currentRightControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.rightController, out currentRightControllerPosition_L, out currentRightControllerRotation_L);
            Matrix4x4 currentRightControllerMatrix_L_Scaled = Matrix4x4.TRS(currentRightControllerPosition_L, currentRightControllerRotation_L, new Vector3(scale, scale, scale));
            Vector3 currentRightControllerPosition_W = (pivot.localToWorldMatrix * currentRightControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

            // initial distance (world space) between the two controllers
            prevDistance = Vector3.Distance(currentLeftControllerPosition_W, currentRightControllerPosition_W);
        }
    }
}
