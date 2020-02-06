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
        public StretchUI lineUI = null;

        [Header("Tweaking")]
        public bool useScaleFactor = false;
        public float nearPlaneFactor = 0.1f;
        public float farPlaneFactor = 5000.0f;
        public float nearPlane = 0.1f; // 10 cm, close enough to not clip the controllers.
        public float farPlane = 1000.0f; // 1km from us, far enough?
        [Tooltip("Player can be xxx times bigger than the world")]
        public float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        [Tooltip("Player can be xxx times smaller than the world")]
        public float minPlayerScale = 50.0f; // world scale = 50.0f;
        private Transform leftHandle = null;
        private Transform pivot = null;
        
        Matrix4x4 initLeftControllerMatrix_WtoL;
        Matrix4x4 initRightControllerMatrix_WtoL;
        Matrix4x4 initMiddleMatrix_WtoL;

        Matrix4x4 initWorldMatrix_W;

        bool isLeftGripped = false;
        bool isRightGripped = false;

        float prevDistance = 0.0f;

        const float deadZone = 0.3f;
        const float fixedScaleFactor = 1.05f;

        float scale;

        Vector3 initCameraPosition;
        Quaternion initCameraRotation;

        bool rotating = false;

        enum ResetType { LEFT_ONLY, LEFT_AND_RIGHT };

        void Start()
        {
            VRInput.TryGetDevices();

            leftHandle = transform.Find("Pivot/LeftHandle");
            if (leftHandle == null) { Debug.LogWarning("Cannot find 'LeftHandle' game object"); }

            pivot = leftHandle.parent; // "Pivot" is the first non-identity parent of right and left controllers.

            if (lineUI == null) { Debug.LogWarning("Cannot find the stretch ui object"); }
            lineUI.Show(false);
            
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
                // left AFTER right, reset all
                if (isRightGripped)
                {
                    ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
                    ResetInitWorldMatrix();
                    ResetDistance(); // after reset world, use scale
                    leftHandle.localScale = Vector3.one; // tmp: show left controller for bi-manual interaction.
                    lineUI.Show(true);
                }
                else // only left, reset left
                {
                    ResetInitControllerMatrices(ResetType.LEFT_ONLY);
                    ResetInitWorldMatrix();
                    leftHandle.localScale = Vector3.zero;
                }

                isLeftGripped = true;
            },
            () =>
            {
                leftHandle.localScale = Vector3.one;

                isLeftGripped = false;
                lineUI.Show(false); // in case we release left grip before right grip
            });

            // TODO: 
            //  * soit on check si ya pas deja une selection ou un outil en cours d'utilisation
            //  * soit on set ici un bool visible par les tools, qui EUX, check qu'on est pas en train de manipuler le monde.
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton,
            () =>
            {
                // right AFTER left, reset all
                if (isLeftGripped)
                {
                    ResetInitControllerMatrices(ResetType.LEFT_AND_RIGHT);
                    ResetInitWorldMatrix();
                    ResetDistance(); // NOTE: called after "reset world", because it uses the scale.

                    leftHandle.localScale = Vector3.one; // tmp: show left controller for bi-manual interaction.

                    lineUI.Show(true);
                }

                isRightGripped = true;
            },
            () =>
            {
                // si on relache le right et que le left est tjs grip, reset left
                if (isLeftGripped)
                {
                    ResetInitControllerMatrices(ResetType.LEFT_ONLY);
                    ResetInitWorldMatrix();

                    leftHandle.localScale = Vector3.zero; // hide controller
                    lineUI.Show(false);
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
                }

                Matrix4x4 transformed; // new world matrix

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

                    
                    Vector3 middlePosition_L = (currentLeftControllerPosition_L + currentRightControllerPosition_L) * 0.5f;
                    Vector3 middleXVector = (currentRightControllerPosition_L - currentLeftControllerPosition_L).normalized;
                    Vector3 middleForwardVector = -Vector3.Cross(middleXVector, pivot.up).normalized;
                    Quaternion middleRotation_L = Quaternion.LookRotation(middleForwardVector, pivot.up);
                    Matrix4x4 middleMatrix_L_Scaled = Matrix4x4.TRS(middlePosition_L, middleRotation_L, new Vector3(scale, scale, scale));

                    Matrix4x4 middleMatrix_W_Delta = pivot.localToWorldMatrix * middleMatrix_L_Scaled * initMiddleMatrix_WtoL;
                    transformed = middleMatrix_W_Delta * initWorldMatrix_W;

                    // scale handling
                    float newDistance = Vector3.Distance(currentLeftControllerPosition_W, currentRightControllerPosition_W);
                    float factor = newDistance / prevDistance;
                    scale *= factor;
                    prevDistance = newDistance;

                    // Rotation for the line text
                    Vector3 middleForward180 = Vector3.Cross(middleXVector, pivot.up).normalized;
                    Vector3 rolledUp = Vector3.Cross(-middleXVector, middleForward180).normalized;
                    Quaternion middleRotationWithRoll_L = Quaternion.LookRotation(middleForward180, rolledUp);
                    Matrix4x4 middleMatrixWithRoll_L_Scaled = Matrix4x4.TRS(middlePosition_L, middleRotationWithRoll_L, new Vector3(scale, scale, scale));
                    Quaternion middleRotationWithRoll_W = (pivot.localToWorldMatrix * middleMatrixWithRoll_L_Scaled).rotation;
                    lineUI.UpdateLineUI(currentLeftControllerPosition_W, currentRightControllerPosition_W, middleRotationWithRoll_W, world.localScale.x);
                }
                else
                {
                    Matrix4x4 currentLeftControllerMatrix_W_Delta = pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled * initLeftControllerMatrix_WtoL;
                    transformed = currentLeftControllerMatrix_W_Delta * initWorldMatrix_W;
                }

                world.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                world.localRotation = transformed.rotation;

                float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 1.0f / minPlayerScale, maxPlayerScale);
                world.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
                // TODO: the following lines can lock you into min or max scale.
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