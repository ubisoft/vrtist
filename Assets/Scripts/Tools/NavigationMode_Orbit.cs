using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{ 
    public class NavigationMode_Orbit : NavigationMode
    {
        public Transform target = null; // the target object, pointed and gripped by the ray.
        public StraightRay ray = null; // the ray object. Put it somewhere like the StretchUI object.

        private bool rayIsColliding = false;

        private float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        private float minPlayerScale = 50.0f; // world scale = 50.0f;

        private float rotationalSpeed = 10.0f;
        private bool rotating = false;

        private Matrix4x4 initLeftControllerMatrix_WtoL;
        private Matrix4x4 initWorldMatrix_W;

        private float scale;
        private bool isLeftGripped = false;

        private const float deadZone = 0.3f;
        private const float fixedScaleFactor = 1.05f; // for grip world scale

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
                ray.gameObject.SetActive(true);
        }

        public override void DeInit()
        {
            base.DeInit();

            if (ray != null)
                ray.gameObject.SetActive(false);
        }

        public override void Update()
        {
            // RAY
            if (ray != null)
            {
                RaycastHit hit;
                Vector3 worldStart = leftHandle.TransformPoint(-0.01f, 0.0f, 0.05f);
                Vector3 worldEnd = leftHandle.TransformPoint(0, 0, 3);
                Vector3 worldDirection = worldEnd - worldStart;
                Ray r = new Ray(worldStart, worldDirection);
                int layersMask = LayerMask.GetMask(new string[] { "Default", "Selection" });
                if (Physics.Raycast(r, out hit, 10.0f, layersMask))
                {
                    rayIsColliding = true;
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(hit.point);
                }
                else
                {
                    rayIsColliding = false;
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(worldEnd);
                }

                // TODO: grip to lock on targetted object/point.
            }


            // TODO: on garde le rotate 45 degres ou on le reserve au mode teleport (et on fait du continu vomitif pour le mode fly)?

            //
            // Joystick -- go forward/backward, and rotate 45 degrees.
            //

            //Vector2 val = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            //if (val != Vector2.zero)
            //{
            //    float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

            //    Vector3 velocity = Camera.main.transform.forward * val.y * d;
            //    camera.position += velocity * flySpeed;

            //    if (Mathf.Abs(val.x) > 0.95f && !rotating)
            //    {
            //        camera.rotation *= Quaternion.Euler(0f, Mathf.Sign(val.x) * 45f, 0f);
            //        rotating = true;
            //    }
            //    if (Mathf.Abs(val.x) <= 0.95f && rotating)
            //    {
            //        rotating = false;
            //    }
            //}

            //
            // LEFT GRIP WORLD (on click)
            //

            //VRInput.ButtonEvent(VRInput.leftController, CommonUsages.gripButton,
            //() =>
            //{
            //    ResetInitControllerMatrices();
            //    ResetInitWorldMatrix();

            //    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
            //    isLeftGripped = true;
            //    GlobalState.IsGrippingWorld = true;
            //},
            //() =>
            //{
            //    SetLeftControllerVisibility(ControllerVisibility.SHOW_NORMAL);
            //    isLeftGripped = false;
            //    GlobalState.IsGrippingWorld = false;
            //});

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

        private void ResetInitControllerMatrices()
        {
            Vector3 initLeftControllerPosition_L;
            Quaternion initLeftControllerRotation_L;
            VRInput.GetControllerTransform(VRInput.leftController, out initLeftControllerPosition_L, out initLeftControllerRotation_L);
            Matrix4x4 initLeftControllerMatrix_L = Matrix4x4.TRS(initLeftControllerPosition_L, initLeftControllerRotation_L, Vector3.one);
            initLeftControllerMatrix_WtoL = (pivot.localToWorldMatrix * initLeftControllerMatrix_L).inverse;
        }

        private void ResetInitWorldMatrix()
        {
            initWorldMatrix_W = world.localToWorldMatrix;
            scale = 1f;
            GlobalState.worldScale = scale;
        }
    }
}
