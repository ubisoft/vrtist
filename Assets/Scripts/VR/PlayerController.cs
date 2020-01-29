using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Base Parameters")]
        public Transform world = null;
        public float playerSpeed = 0.2f;

        private Transform leftHandle = null;
        private Transform pivot = null;
        private LineRenderer line = null;

        Matrix4x4 initLeftControllerMatrix_WtoL;
        Matrix4x4 initRightControllerMatrix_WtoL;

        Matrix4x4 initWorldMatrix_W;

        // TODO: de we nee a "just gripped" ?
        bool isLeftGripped = false;
        bool isRightGripped = false;

        float prevDistance = 0.0f;

        const float deadZone = 0.3f;
        const float fixedScaleFactor = 1.05f;

        float scale;

        Vector3 initCameraPosition;
        Quaternion initCameraRotation;

        bool rotating = false;

        void Start()
        {
            VRInput.TryGetDevices();

            leftHandle = transform.Find("Pivot/LeftHandle");
            if (leftHandle == null) { Debug.LogWarning("Cannot find 'LeftHandle' game object"); }

            pivot = leftHandle.parent; // "Pivot" is the first non-identity parent of right and left controllers.

            line = gameObject.GetComponent<LineRenderer>();
            line.enabled = false;
            line.startWidth = 0.005f;
            line.endWidth = 0.005f;

            initCameraPosition = transform.position;
            initCameraRotation = transform.rotation;
            UpdateCameraClipPlanes();

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Trigger, "Display Palette");
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Primary, "Undo");
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Secondary, "Redo");
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Move / Turn");
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Grip, "Grip World");
        }

        void UpdateCameraClipPlanes()
        {
            Camera.main.nearClipPlane = 0.1f * world.localScale.x;
            Camera.main.farClipPlane = 5000f * world.localScale.x;
        }

        // Update is called once per frame
        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                UpdateNavigation();

                UpdatePalette();

                VRInput.ButtonEvent(VRInput.leftController, CommonUsages.primaryButton, () => { },
                () =>
                {
                    CommandManager.Undo();
                });
                VRInput.ButtonEvent(VRInput.leftController, CommonUsages.secondaryButton, () => { },
                () =>
                {
                    CommandManager.Redo();
                });
            }

        }

        private void UpdateNavigation()
        {
            if (!leftHandle.gameObject.activeSelf)
            {
                leftHandle.gameObject.SetActive(true);
            }

            // Update controller transform
            VRInput.UpdateTransformFromVRDevice(leftHandle, VRInput.leftController);

            // Left joystick: 
            //  - Vertical = Fly forward/backwards
            //  - Horizontal = Rotate by 45 degrees steps
            Navigation_FlyRotate();

            // Left joystick CLICK = Reset position
            // TODO: FIT instead of reset.
            Navigation_Reset();

            // grip world
            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.gripButton,
            () =>
            {
                if (isRightGripped)
                {
                    ResetInitLeftControllerMatrix();
                    ResetInitRightControllerMatrix();
                    ResetInitWorldMatrix();
                    ResetDistance(); // after reset world, use scale
                    leftHandle.localScale = Vector3.one; // tmp: show left controller for bi-manual interaction.
                    line.enabled = true;
                }
                else
                {
                    ResetInitLeftControllerMatrix();
                    ResetInitWorldMatrix();
                    leftHandle.localScale = Vector3.zero;
                }

                isLeftGripped = true;
            },
            () =>
            {
                leftHandle.localScale = Vector3.one;

                isLeftGripped = false;
                line.enabled = false; // in case we release left grip before right grip
            });


            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton,
            () =>
            {
                if (isLeftGripped)// TODO: && no selection gripped by right controller
                {
                    ResetInitLeftControllerMatrix();
                    ResetInitRightControllerMatrix();
                    ResetInitWorldMatrix();
                    ResetDistance(); // after reset world, use scale
                    leftHandle.localScale = Vector3.one; // tmp: show left controller for bi-manual interaction.

                    line.enabled = true;
                }

                isRightGripped = true;
            },
            () =>
            {
                // si on relache le right et que le left est tjs grip, reset left
                if (isLeftGripped)
                {
                    ResetInitLeftControllerMatrix();
                    ResetInitWorldMatrix();

                    leftHandle.localScale = Vector3.zero; // hide controller
                    line.enabled = false;
                }

                isRightGripped = false;
            });

            if (VRInput.GetValue(VRInput.leftController, CommonUsages.grip) > deadZone)
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
                }

                // update left joystick
                Vector3 currentLeftControllerPosition_L;
                Quaternion currentLeftControllerRotation_L;
                VRInput.GetControllerTransform(VRInput.leftController, out currentLeftControllerPosition_L, out currentLeftControllerRotation_L);
                Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));
                Vector3 currentLeftControllerPosition_W = (pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

                if (isRightGripped)
                {
                    // update right joystick
                    Vector3 currentRightControllerPosition_L;
                    Quaternion currentRightControllerRotation_L;
                    VRInput.GetControllerTransform(VRInput.rightController, out currentRightControllerPosition_L, out currentRightControllerRotation_L);
                    Matrix4x4 currentRightControllerMatrix_L_Scaled = Matrix4x4.TRS(currentRightControllerPosition_L, currentRightControllerRotation_L, new Vector3(scale, scale, scale));
                    Vector3 currentRightControllerPosition_W = (pivot.localToWorldMatrix * currentRightControllerMatrix_L_Scaled).MultiplyPoint(Vector3.zero);

                    Vector3 currentMiddleControllerPosition_W = (currentLeftControllerPosition_W + currentRightControllerPosition_W) * 0.5f;

                    line.SetPosition(0, currentLeftControllerPosition_W);
                    line.SetPosition(1, currentMiddleControllerPosition_W);
                    line.SetPosition(2, currentRightControllerPosition_W);

                    // scale handling
                    float newDistance = Vector3.Distance(currentLeftControllerPosition_W, currentRightControllerPosition_W);
                    float factor = newDistance / prevDistance;
                    scale *= factor;
                    prevDistance = newDistance;
                }

                Matrix4x4 currentLeftControllerMatrix_W_Delta = pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled * initLeftControllerMatrix_WtoL;


                Matrix4x4 transformed = currentLeftControllerMatrix_W_Delta * initWorldMatrix_W;
                world.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                world.localRotation = transformed.rotation;

                float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 0.0005f, 50f);
                world.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
                if (transformed.lossyScale.x != clampedScale)
                {
                    scale = prevScale;
                }

                UpdateCameraClipPlanes();
            }
        }

        private void Navigation_Reset()
        {
            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.primary2DAxisClick,
            () =>
            {
                world.localPosition = Vector3.zero;
                world.localRotation = Quaternion.identity;
                world.localScale = Vector3.one;

                transform.position = initCameraPosition;
                transform.rotation = initCameraRotation;
            });
        }

        private void Navigation_FlyRotate()
        {
            Vector2 val = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            if (val != Vector2.zero)
            {
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                Vector3 velocity = Camera.main.transform.forward * val.y * d;
                transform.position += velocity * playerSpeed;

                if (Mathf.Abs(val.x) > 0.95f && !rotating)
                {
                    transform.rotation *= Quaternion.Euler(0f, Mathf.Sign(val.x) * 45f, 0f);
                    rotating = true;
                }
                if (Mathf.Abs(val.x) <= 0.95f && rotating)
                {
                    rotating = false;
                }
            }
        }

        private void ResetInitLeftControllerMatrix()
        {
            Vector3 initLeftControllerPosition_L; // initial left controller position in local space.
            Quaternion initLeftControllerRotation_L; // initial left controller rotation in local space.
            VRInput.GetControllerTransform(VRInput.leftController, out initLeftControllerPosition_L, out initLeftControllerRotation_L);

            Matrix4x4 initLeftControllerMatrix_L = Matrix4x4.TRS(initLeftControllerPosition_L, initLeftControllerRotation_L, Vector3.one);
            initLeftControllerMatrix_WtoL = (pivot.localToWorldMatrix * initLeftControllerMatrix_L).inverse;
        }

        private void ResetInitRightControllerMatrix()
        {
            Vector3 initRightControllerPosition_L; // initial right controller position in local space.
            Quaternion initRightControllerRotation_L; // initial right controller rotation in local space.
            VRInput.GetControllerTransform(VRInput.rightController, out initRightControllerPosition_L, out initRightControllerRotation_L);

            Matrix4x4 initRightControllerMatrix_L = Matrix4x4.TRS(initRightControllerPosition_L, initRightControllerRotation_L, Vector3.one);
            initRightControllerMatrix_WtoL = (pivot.localToWorldMatrix * initRightControllerMatrix_L).inverse;
        }

        private void ResetInitWorldMatrix()
        {
            initWorldMatrix_W = world.localToWorldMatrix;
            scale = 1f;
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

        private void UpdatePalette()
        {
            if (VRInput.GetValue(VRInput.leftController, CommonUsages.trigger) > deadZone)
            {
                ToolsUIManager.Instance.EnableMenu(true);
            }
            else
            {
                ToolsUIManager.Instance.EnableMenu(false);
            }
        }
    }
}