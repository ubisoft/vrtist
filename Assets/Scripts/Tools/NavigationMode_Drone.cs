using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class NavigationMode_Drone : NavigationMode
    {
        private float flySpeed = 0.03f;
        private float rotationSpeed = 0.3f;

        private List<Vector4> prevJoysticksStates = new List<Vector4>();
        private List<float> deltaTimes = new List<float>();

        private Vector3 cameraForward;

        public override void Init(Transform rigTransform, Transform worldTransform, Transform leftHandleTransform, Transform rightHandleTransform, Transform pivotTransform, Transform cameraTransform, Transform parametersTransform)
        {
            base.Init(rigTransform, worldTransform, leftHandleTransform, rightHandleTransform, pivotTransform, cameraTransform, parametersTransform);

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

        private Vector4 GetJoysticksValue()
        {
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            Vector2 rightJoyValue = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
            Vector4 currentValue = new Vector4(leftJoyValue.x, leftJoyValue.y, rightJoyValue.x, rightJoyValue.y);

            float damping = options.flightDamping * 5f;
            int elemCount = (int)damping;

            int currentSize = prevJoysticksStates.Count;
            if (currentSize > elemCount)
            {
                prevJoysticksStates.RemoveRange(0, currentSize - elemCount);
                deltaTimes.RemoveRange(0, currentSize - elemCount);
            }

            prevJoysticksStates.Add(currentValue);
            deltaTimes.Add(Time.deltaTime);

            Vector4 average = Vector4.zero;
            float invCount = 1f / (float)prevJoysticksStates.Count;

            float dtSum = 0;
            foreach (float dt in deltaTimes)
                dtSum += dt;

            float invDtSum = 1f / dtSum;
            for(int i = 0; i < prevJoysticksStates.Count; i++)
            {
                average += prevJoysticksStates[i] * deltaTimes[i] * invDtSum;
            }

            return average;
        }

        // Update is called once per frame
        public override void Update()
        {
            float speed = flySpeed * options.flightSpeed;
            Vector4 joystickValue = GetJoysticksValue();

            Vector2 leftJoyValue = new Vector2(joystickValue.x, joystickValue.y);
            if (leftJoyValue != Vector2.zero)
            {
                float rSpeed = rotationSpeed * options.flightRotationSpeed;
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

            Vector2 rightJoyValue = new Vector2(joystickValue.z, joystickValue.w);
            if (rightJoyValue != Vector2.zero)
            {
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                // move forward
                Vector3 up = Vector3.up;
                Vector3 right = Vector3.Cross(up, cameraForward).normalized;
                Vector3 forward = Vector3.Cross(right, up).normalized;
                Vector3 forwardVelocity = forward * rightJoyValue.y * d;

                // strafe
                Vector3 leftRightVelocity = right * rightJoyValue.x * d;

                rig.position += forwardVelocity * speed + leftRightVelocity * speed;
            }
        }
    }
}
