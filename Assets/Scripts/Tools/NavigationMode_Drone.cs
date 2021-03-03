/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
            Tooltips.SetText(VRDevice.SecondaryController, Tooltips.Location.Joystick, Tooltips.Action.Joystick, "Altitude / Strafe");

            usedControls = UsedControls.LEFT_JOYSTICK | UsedControls.RIGHT_JOYSTICK;

            Transform drone = parameters.Find("Drone");
            drone.gameObject.SetActive(true);
        }

        public override void DeInit()
        {
            Transform drone = parameters.Find("Drone");
            drone.gameObject.SetActive(false);
        }

        private Vector4 GetJoysticksValue()
        {
            Vector2 leftJoyValue = VRInput.GetValue(VRInput.secondaryController, CommonUsages.primary2DAxis);
            Vector2 rightJoyValue = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
            Vector4 currentValue = new Vector4(leftJoyValue.x, leftJoyValue.y, rightJoyValue.x, rightJoyValue.y);

            float damping = options.flightDamping * 5f;
            int elemCount = (int) damping;

            int currentSize = prevJoysticksStates.Count;
            if (currentSize > elemCount)
            {
                prevJoysticksStates.RemoveRange(0, currentSize - elemCount);
                deltaTimes.RemoveRange(0, currentSize - elemCount);
            }

            prevJoysticksStates.Add(currentValue);
            deltaTimes.Add(Time.deltaTime);

            Vector4 average = Vector4.zero;
            float invCount = 1f / (float) prevJoysticksStates.Count;

            float dtSum = 0;
            foreach (float dt in deltaTimes)
                dtSum += dt;

            float invDtSum = 1f / dtSum;
            for (int i = 0; i < prevJoysticksStates.Count; i++)
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
