using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class NavigationMode_BiManual : NavigationMode
    {
        private StretchUI lineUI = null;
        private float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        private float minPlayerScale = 50000.0f; // world scale = 5000.0f;

        private Matrix4x4 initLeftControllerMatrix_WtoL;
        private Matrix4x4 initRightControllerMatrix_WtoL;
        private Matrix4x4 initMiddleMatrix_WtoL;

        private Matrix4x4 initWorldMatrix_W;
        private Matrix4x4 initPivotMatrix;

        private bool isLeftGripped = false;
        private bool isRightGripped = false;

        private float prevDistance = 0.0f;
        private float scale;

        private const float deadZone = 0.3f;
        private const float fixedScaleFactor = 1.05f; // for grip world scale

        enum ResetType { LEFT_ONLY, LEFT_AND_RIGHT };

        public NavigationMode_BiManual(StretchUI line, float minScale, float maxScale)
        {
            lineUI = line;
            minPlayerScale = minScale;
            maxPlayerScale = maxScale;
        }

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform rightHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, rightHandleTransform, pivotTransform, cameraTransform, parametersTransform);

            lineUI.Show(false);

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Grip, "Grip World");

            usedControls = UsedControls.LEFT_GRIP | UsedControls.RIGHT_GRIP;
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
                if (isRightGripped) // && Selection.selection.Count == 0)
                {
                    ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
                    ResetInitWorldMatrix();
                    ResetDistance(); // after reset world, use scale

                    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);

                    lineUI.Show(true, StretchUI.LineMode.DOUBLE);
                    GlobalState.IsGrippingWorld = true;
                    ToolsManager.ActivateCurrentTool(false);
                }

                isLeftGripped = true;
            },
            () =>
            {
                SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);

                lineUI.Show(false);
                ToolsManager.ActivateCurrentTool(true);
                GlobalState.IsGrippingWorld = false;

                isLeftGripped = false;
            });

            //
            // RIGHT GRIP WORLD
            //

            // NOTE: On ne peut predire dans quel ordre les Update vont s'executer. Le Selector/SelectorTrigger peuvent
            //       recuperer le LeftGrip avant nous, et commencer a grip un objet avant qu'on ait pu set la property
            //       GlobalState.IsGrippingWorld. Cela pose-t-il encore un probleme?
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton,
            () =>
            {
                //if (Selection.selection.Count == 0)
                {
                    // right AFTER left and no selection, reset all
                    if (isLeftGripped)
                    {
                        ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
                        ResetInitWorldMatrix();
                        ResetDistance(); // NOTE: called after "reset world", because it uses the scale.

                        SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
                        lineUI.Show(true, StretchUI.LineMode.DOUBLE);
                        GlobalState.IsGrippingWorld = true;
                        ToolsManager.ActivateCurrentTool(false);
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

                    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
                }
                //lineUI.Show(true, StretchUI.LineMode.SINGLE);
                ToolsManager.ActivateCurrentTool(true);
                GlobalState.IsGrippingWorld = false;

                isRightGripped = false;
            });

            // NOTE: we test isLeftGrip because we can be ungripped but still over the deadzone, strangely.
            if (isLeftGripped && VRInput.GetValue(VRInput.leftController, CommonUsages.grip) > deadZone)
            {
                if (isRightGripped)
                {
                    float prevScale = scale;

                    VRInput.GetControllerTransform(VRInput.leftController, out Vector3 currentLeftControllerPosition_L, out Quaternion currentLeftControllerRotation_L);
                    VRInput.GetControllerTransform(VRInput.rightController, out Vector3 currentRightControllerPosition_L, out Quaternion currentRightControllerRotation_L);

                    Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));
                    Matrix4x4 currentLeftControllerMatrix_W = initPivotMatrix * currentLeftControllerMatrix_L_Scaled;
                    Vector3 currentLeftControllerPosition_W = currentLeftControllerMatrix_W.MultiplyPoint(Vector3.zero);

                    // update right joystick
                    Matrix4x4 currentRightControllerMatrix_L_Scaled = Matrix4x4.TRS(currentRightControllerPosition_L, currentRightControllerRotation_L, new Vector3(scale, scale, scale));
                    Vector3 currentRightControllerPosition_W = (initPivotMatrix * currentRightControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

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

                    Matrix4x4 middleMatrix_L_Scaled = Matrix4x4.TRS(middlePosition_L, middleRotation_L, Vector3.one) * Matrix4x4.Scale(Vector3.one * scale);
                    Matrix4x4 middleMatrix_W_Delta = initPivotMatrix * middleMatrix_L_Scaled * initMiddleMatrix_WtoL;
                    Matrix4x4 transformed = middleMatrix_W_Delta * initWorldMatrix_W;
                    transformed = transformed.inverse;

                    float s = 1.0f;
                    float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 1.0f / maxPlayerScale, minPlayerScale);
                    if (transformed.lossyScale.x == clampedScale)
                    {
                        // translate/rotate/scale using the new scale
                        rig.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                        rig.localRotation = transformed.rotation;
                        rig.localScale = new Vector3(clampedScale, clampedScale, clampedScale);

                        s = oldScale;
                    }

                    // Get head position
                    VRInput.GetControllerTransform(VRInput.head, out Vector3 HeadPosition, out Quaternion headRotation);
                    Matrix4x4 invHeadMatrix = Matrix4x4.TRS(HeadPosition, headRotation, Vector3.one).inverse;
                    // Project left & right controller into head matrix to determine which one is on the left
                    Vector3 leftControllerInHeadMatrix = invHeadMatrix.MultiplyPoint(currentLeftControllerPosition_L);
                    Vector3 rightControllerInHeadMatrix = invHeadMatrix.MultiplyPoint(currentRightControllerPosition_L);
                    // reverse text if right and left hands are crossed
                    if (leftControllerInHeadMatrix.x > rightControllerInHeadMatrix.x)
                    {
                        middleXVector = -middleXVector;
                    }

                    // Rotation for the line text
                    Vector3 middleForward180 = Vector3.Cross(middleXVector, pivot.up).normalized;
                    Vector3 rolledUp = Vector3.Cross(-middleXVector, middleForward180).normalized;
                    Quaternion middleRotationWithRoll_L = Quaternion.LookRotation(middleForward180, rolledUp);
                    Matrix4x4 middleMatrixWithRoll_L_Scaled = Matrix4x4.TRS(middlePosition_L, middleRotationWithRoll_L, new Vector3(s, s, s));
                    Quaternion middleRotationWithRoll_W = (pivot.localToWorldMatrix * middleMatrixWithRoll_L_Scaled).rotation;

                    lineUI.UpdateLineUI(pivot.TransformPoint(currentLeftControllerPosition_L), pivot.TransformPoint(currentRightControllerPosition_L), middleRotationWithRoll_W, rig.localScale.x);
                }
                GlobalState.WorldScale = 1f / rig.localScale.x;

                UpdateCameraClipPlanes();
            }
        }

        private void ResetInitControllerMatrices(ResetType res)
        {
            Vector3 initLeftControllerPosition_L;
            Quaternion initLeftControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.leftController, out initLeftControllerPosition_L, out initLeftControllerRotation_L);
            Matrix4x4 initLeftControllerMatrix_L = Matrix4x4.TRS(initLeftControllerPosition_L, initLeftControllerRotation_L, Vector3.one);
            initPivotMatrix = Matrix4x4.TRS(pivot.localPosition, pivot.localRotation, pivot.localScale);
            initLeftControllerMatrix_WtoL = (initPivotMatrix * initLeftControllerMatrix_L).inverse;

            if (res == ResetType.LEFT_AND_RIGHT)
            {
                Vector3 initRightControllerPosition_L; // initial right controller position in local space.
                Quaternion initRightControllerRotation_L; // initial right controller rotation in local space.
                VRInput.GetControllerTransform(VRInput.rightController, out initRightControllerPosition_L, out initRightControllerRotation_L);
                Matrix4x4 initRightControllerMatrix_L = Matrix4x4.TRS(initRightControllerPosition_L, initRightControllerRotation_L, Vector3.one);
                initRightControllerMatrix_WtoL = (initPivotMatrix * initRightControllerMatrix_L).inverse;

                Vector3 initMiddlePosition_L = (initLeftControllerPosition_L + initRightControllerPosition_L) * 0.5f;
                Vector3 middleXVector = (initRightControllerPosition_L - initLeftControllerPosition_L).normalized;
                Vector3 middleForwardVector = -Vector3.Cross(middleXVector, pivot.up).normalized;
                Quaternion initMiddleRotation_L = Quaternion.LookRotation(middleForwardVector, pivot.up);
                Matrix4x4 initMiddleMatrix_L = Matrix4x4.TRS(initMiddlePosition_L, initMiddleRotation_L, Vector3.one);
                initMiddleMatrix_WtoL = (initPivotMatrix * initMiddleMatrix_L).inverse;
            }
        }

        private void ResetInitWorldMatrix()
        {
            initWorldMatrix_W = rig.worldToLocalMatrix;
            scale = 1f;
            GlobalState.WorldScale = scale;
        }

        private void ResetDistance()
        {
            // compute left controller world space position
            Vector3 currentLeftControllerPosition_L;
            Quaternion currentLeftControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.leftController, out currentLeftControllerPosition_L, out currentLeftControllerRotation_L);
            Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));
            Vector3 currentLeftControllerPosition_W = (initPivotMatrix * currentLeftControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

            // compute right controller world space position
            Vector3 currentRightControllerPosition_L;
            Quaternion currentRightControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.rightController, out currentRightControllerPosition_L, out currentRightControllerRotation_L);
            Matrix4x4 currentRightControllerMatrix_L_Scaled = Matrix4x4.TRS(currentRightControllerPosition_L, currentRightControllerRotation_L, new Vector3(scale, scale, scale));
            Vector3 currentRightControllerPosition_W = (initPivotMatrix * currentRightControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

            // initial distance (world space) between the two controllers
            prevDistance = Vector3.Distance(currentLeftControllerPosition_W, currentRightControllerPosition_W);
        }
    }
}
