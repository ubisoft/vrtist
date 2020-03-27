using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{

    public class NavigationMode_Drone : NavigationMode
    {
        private float flySpeed = 0.03f;
        private float rotationSpeed = 1f;

        private Vector3 cameraForward;
        public override bool IsCompatibleWithPalette()
        {
            return false;
        }

        public override bool IsCompatibleWithUndoRedo()
        {
            return false;
        }

        public override bool IsCompatibleWithReset()
        {
            return false;
        }

        public override void Init(Transform cameraTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform)
        {
            base.Init(cameraTransform, worldTransform, leftHandleTransform, pivotTransform);
            cameraForward = Camera.main.transform.TransformDirection(Vector3.forward).normalized;
            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Altitude / Strafe");
            Tooltips.CreateTooltip(leftHandle.Find("right_controller").gameObject, Tooltips.Anchors.Joystick, "Move Forward, Backward / Turn");
        }

        // Update is called once per frame
        public override void Update()
        {
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            if (leftJoyValue != Vector2.zero)
            {
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                // move up
                Vector3 up = Vector3.up;
                Vector3 upDownVelocity = up * leftJoyValue.y * d;
                Vector3 right = Vector3.Cross(up, cameraForward).normalized;

                camera.position += upDownVelocity * flySpeed;

                // rotate
                Quaternion rotation = Quaternion.AngleAxis(leftJoyValue.x * rotationSpeed, up);
                camera.rotation = rotation * camera.rotation;

                // update forward
                Matrix4x4 m = new Matrix4x4();
                m.SetColumn(0, right);
                m.SetColumn(1, up);
                m.SetColumn(2, cameraForward);
                m.SetColumn(3, new Vector4(0, 0, 0, 1));
                Matrix4x4 rotated = Matrix4x4.Rotate(rotation) * m;
                cameraForward = rotated.GetColumn(2).normalized;                
            }

            Vector2 rightJoyValue = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
            if (rightJoyValue != Vector2.zero)
            {
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                // move forward
                Vector3 up = Vector3.up;
                Vector3 right = Vector3.Cross(up, cameraForward).normalized;
                Vector3 forwardVelocity = cameraForward * rightJoyValue.y * d;

                // strafe
                Vector3 leftRightVelocity = right * rightJoyValue.x * d;

                camera.position += forwardVelocity * flySpeed + leftRightVelocity * flySpeed;
            }
        }
    }

}