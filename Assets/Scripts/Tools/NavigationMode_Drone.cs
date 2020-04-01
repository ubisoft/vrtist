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

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, pivotTransform, cameraTransform, parametersTransform);
            cameraForward = Camera.main.transform.TransformDirection(Vector3.forward).normalized;
            // Create tooltips
            Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Joystick, "Altitude / Strafe");
            // TODO: trouver un moyen d'aller changer un tooltip sur le right_controller (le bon, il y en a un par outil), et lui remettre
            // ses tooltips quand on change d'outil.
            //Tooltips.CreateTooltip(leftHandle.Find("right_controller").gameObject, Tooltips.Anchors.Joystick, "Move Forward, Backward / Turn");

            usedControls = UsedControls.LEFT_JOYSTICK | UsedControls.RIGHT_JOYSTICK;

            Transform drone = parametersTransform.Find("Drone");
            drone.gameObject.SetActive(true);
        }

        public override void DeInit() 
        {
            Transform drone = parameters.Find("Drone");
            drone.gameObject.SetActive(false);
        }

        // Update is called once per frame
        public override void Update()
        {
            float speed = flySpeed * GlobalState.flightSpeed;
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            if (leftJoyValue != Vector2.zero)
            {
                float rSpeed = rotationSpeed * GlobalState.flightRotationSpeed;
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                // move up
                Vector3 up = Vector3.up;
                Vector3 upDownVelocity = up * leftJoyValue.y * d;
                Vector3 right = Vector3.Cross(up, cameraForward).normalized;

                rig.position += upDownVelocity * speed;

                // rotate
                Quaternion rotation = Quaternion.AngleAxis(leftJoyValue.x * rSpeed, up);
                rig.rotation = rotation * rig.rotation;

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

                rig.position += forwardVelocity * speed + leftRightVelocity * speed;
            }
        }
    }
}
