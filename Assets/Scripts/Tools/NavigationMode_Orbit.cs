using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{ 
    public class NavigationMode_Orbit : NavigationMode
    {
        public StraightRay ray = null; // the ray object. Put it somewhere like the StretchUI object.
        public bool limitVertical = true;

        private bool isLocked = false;
        private float distance = 0.0f;
        
        // TODO: handle distance from object, and world scale using the right joystick.

        private Transform target = null; // the target object, pointed and gripped by the ray.
        private Vector3 targetPosition = Vector3.zero; // the target object, pointed and gripped by the ray.

        private float rotationalSpeed = 3.0f;
        private const float deadZone = 0.5f;

        private float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        private float minPlayerScale = 50.0f; // world scale = 50.0f;

        public NavigationMode_Orbit(StraightRay theRay, float speed, float minScale, float maxScale)
        {
            ray = theRay;
            rotationalSpeed = speed;
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

            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Turn");
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Grip, "Grip Object");

            // How to go closer/farther, and change scale? Right joystick?

            if (ray != null)
            {
                ray.gameObject.SetActive(true);
                ray.SetDefaultColor();
            }
        }

        public override void DeInit()
        {
            base.DeInit();

            if (ray != null)
            {
                ray.gameObject.SetActive(false);
            }
        }

        public override void Update()
        {
            if (ray == null)
                return;

            //
            // RAY - collision with scene objects.
            //
            if (!isLocked)
            {
                RaycastHit hit;
                Vector3 worldStart = leftHandle.TransformPoint(-0.01f, 0.0f, 0.05f);
                Vector3 worldEnd = leftHandle.TransformPoint(0, 0, 3);
                Vector3 worldDirection = worldEnd - worldStart;
                Ray r = new Ray(worldStart, worldDirection);
                int layersMask = LayerMask.GetMask(new string[] { "Default", "Selection" });
                if (Physics.Raycast(r, out hit, 10.0f, layersMask))
                {

                    target = hit.collider.transform;
                    targetPosition = hit.collider.bounds.center;
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(hit.point);
                }
                else
                {
                    target = null;
                    targetPosition = Vector3.zero;
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(worldEnd);
                }
            }
            else 
            {
                //
                // Joystick -- left/right = rotate left/right.
                //             up/down = rotate up/down.

                Vector2 val = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
                if (val != Vector2.zero)
                {
                    // Horizontal rotation
                    //Vector3 up = Vector3.up;
                    //Vector3 up = camera.up;
                    Vector3 up = world.up;
                    //Vector3 up = pivot.up;
                    if (Mathf.Abs(val.x) > deadZone)
                    {
                        float value = Mathf.Sign(val.x) * (Mathf.Abs(val.x) - deadZone) / (1.0f - deadZone); // remap
                        float rotate_amount_h = value * rotationalSpeed;
                        world.RotateAround(targetPosition, up, rotate_amount_h);
                    }

                    // Vertical rotation
                    Vector3 forward = Vector3.Normalize(pivot.position - targetPosition);
                    Vector3 right = Vector3.Cross(up, forward);

                    if (Mathf.Abs(val.y) > deadZone)
                    {
                        float value = Mathf.Sign(val.y) * (Mathf.Abs(val.y) - deadZone)/ (1.0f - deadZone); // remap
                        float dot = Vector3.Dot(up, forward);
                        bool in_safe_zone = (Mathf.Abs(dot) < 0.8f);
                        bool above_but_going_down = (dot > 0.8f) && (value < 0.0f);
                        bool below_but_going_up = (dot < -0.8f) && (value > 0.0f);
                        if (!limitVertical || in_safe_zone || above_but_going_down || below_but_going_up) // only within limits
                        {
                            float rotate_amount_v = value * rotationalSpeed;
                            world.RotateAround(targetPosition, right, rotate_amount_v);
                        }
                    }
                }

                // Position the ray AFTER the rotation of the camera, to avoid a one frame shift.
                ray.SetStartPosition(leftHandle.TransformPoint(-0.01f, 0.0f, 0.05f));
                ray.SetEndPosition(targetPosition);
            }


            //
            // LEFT GRIP (click) - lock on targetted object/point.
            //

            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.gripButton,
            () =>
            {
                if (target != null)
                {
                    isLocked = true;
                    ray.SetActiveColor();
                    distance = Vector3.Distance(targetPosition, camera.position);
                }

                GlobalState.IsGrippingWorld = true;
            },
            () =>
            {
                isLocked = false;
                ray.SetDefaultColor();
                GlobalState.IsGrippingWorld = false;
            });






            // NOTE: we test isLeftGrip because we can be ungripped but still over the deadzone, strangely.
            //if (isLeftGripped && VRInput.GetValue(VRInput.leftController, CommonUsages.grip) > deadZone)
            //{
            //    float prevScale = scale;

            //    // Scale using left joystick.
            //    Vector2 joystickAxis = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            //    if (joystickAxis.y > deadZone)
            //        scale *= fixedScaleFactor;
            //    if (joystickAxis.y < -deadZone)
            //        scale /= fixedScaleFactor;

            //    GlobalState.worldScale = scale;

            //    // TODO: draw scale factor.

            //    // update left joystick
            //    Vector3 currentLeftControllerPosition_L;
            //    Quaternion currentLeftControllerRotation_L;
            //    VRInput.GetControllerTransform(VRInput.leftController, out currentLeftControllerPosition_L, out currentLeftControllerRotation_L);
            //    Matrix4x4 currentLeftControllerMatrix_L_Scaled = Matrix4x4.TRS(currentLeftControllerPosition_L, currentLeftControllerRotation_L, new Vector3(scale, scale, scale));
            //    Matrix4x4 currentLeftControllerMatrix_W = pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled;
            //    Vector3 currentLeftControllerPosition_W = currentLeftControllerMatrix_W.MultiplyPoint(Vector3.zero);

            //    Matrix4x4 currentLeftControllerMatrix_W_Delta = pivot.localToWorldMatrix * currentLeftControllerMatrix_L_Scaled * initLeftControllerMatrix_WtoL;
            //    Matrix4x4 transformed = currentLeftControllerMatrix_W_Delta * initWorldMatrix_W;

            //    world.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
            //    world.localRotation = transformed.rotation;
            //    float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 1.0f / maxPlayerScale, minPlayerScale);
            //    world.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
            //    if (transformed.lossyScale.x != clampedScale)
            //    {
            //        scale = prevScale;
            //    }

            //    GlobalState.worldScale = world.localScale.x;

            //    UpdateCameraClipPlanes();
            //}
        }
    }
}
