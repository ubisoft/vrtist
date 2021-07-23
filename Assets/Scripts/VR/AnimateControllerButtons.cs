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

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AnimateControllerButtons : MonoBehaviour
    {
        private Transform gripTransform = null;
        private float gripRotationAmplitude = 15.0f;
        private Quaternion initGripRotation = Quaternion.identity;

        private Transform triggerTransform = null;
        private float triggerRotationAmplitude = 15.0f;
        private Quaternion initTriggerRotation = Quaternion.identity;

        private Transform joystickTransform = null;
        private float joystickRotationAmplitude = 15.0f;
        private Quaternion initJoystickRotation = Quaternion.identity;

        private Transform primaryTransform = null;
        private float primaryTranslationAmplitude = -0.0016f;
        private Vector3 initPrimaryTranslation = Vector3.zero;

        private Transform secondaryTransform = null;
        private float secondaryTranslationAmplitude = -0.0016f;
        private Vector3 initSecondaryTranslation = Vector3.zero;

        public bool rightHand = true;
        private InputDevice device;

        public float gripDirection = 1.0f;

        // Start is called before the first frame update
        void Start()
        {
            CaptureController();
            CaptureInitialTransforms();
        }

        private void CaptureController()
        {
            if (rightHand)
            {
                device = VRInput.primaryController;
            }
            else
            {
                device = VRInput.secondaryController;
            }
        }

        private void CaptureInitialTransforms()
        {
            gripTransform = transform.Find("GripButtonPivot/GripButton");
            if (null != gripTransform)
            {
                initGripRotation = gripTransform.localRotation;
            }

            triggerTransform = transform.Find("TriggerButtonPivot/TriggerButton");
            if (null != triggerTransform)
            {
                initTriggerRotation = triggerTransform.localRotation;
            }

            joystickTransform = transform.Find("PrimaryAxisPivot/PrimaryAxis");
            if (null != joystickTransform)
            {
                initJoystickRotation = joystickTransform.localRotation;
            }

            primaryTransform = transform.Find("PrimaryButtonPivot/PrimaryButton");
            if (null != primaryTransform)
            {
                initPrimaryTranslation = primaryTransform.localPosition;
            }

            secondaryTransform = transform.Find("SecondaryButtonPivot/SecondaryButton");
            if (null != secondaryTransform)
            {
                initSecondaryTranslation = secondaryTransform.localPosition;
            }
        }

        public void OnRightHanded(bool isRightHanded)
        {
            // TODO: handle what needs to be handled when we change hands.

            //gripDirection = isRightHanded ? 1.0f : -1.0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (!device.isValid)
            {
                CaptureController();
                CaptureInitialTransforms();
            }

            // GRIP
            if (null != gripTransform)
            {
                float gripAmount = VRInput.GetValue(device, CommonUsages.grip);
                gripTransform.localRotation = initGripRotation * Quaternion.Euler(0, gripAmount * gripRotationAmplitude * gripDirection, 0);
                gripTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gripAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
            }

            // TRIGGER
            if (null != triggerTransform)
            {
                float triggerAmount = VRInput.GetValue(device, CommonUsages.trigger);
                triggerTransform.localRotation = initTriggerRotation * Quaternion.Euler(triggerAmount * triggerRotationAmplitude, 0, 0);
                triggerTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", triggerAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
            }

            // JOYSTICK
            if (null != joystickTransform)
            {
                Vector2 joystick = VRInput.GetValue(device, CommonUsages.primary2DAxis);
                joystickTransform.localRotation = initJoystickRotation * Quaternion.Euler(joystick.y * joystickRotationAmplitude, 0, joystick.x * -joystickRotationAmplitude);
                joystickTransform.gameObject.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", joystick.magnitude > 0.05f ? UIOptions.SelectedColor : Color.black);
            }

            // PRIMARY
            if (null != primaryTransform)
            {
                bool primaryState = VRInput.GetValue(device, CommonUsages.primaryButton);
                primaryTransform.localPosition = initPrimaryTranslation;
                primaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", primaryState ? UIOptions.SelectedColor : Color.black);
                if (primaryState)
                {
                    primaryTransform.localPosition += new Vector3(0, 0, primaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
                }
            }

            // SECONDARY
            if (null != secondaryTransform)
            {
                bool secondaryState = VRInput.GetValue(device, CommonUsages.secondaryButton);
                secondaryTransform.localPosition = initSecondaryTranslation;
                secondaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", secondaryState ? UIOptions.SelectedColor : Color.black);
                if (secondaryState)
                {
                    secondaryTransform.localPosition += new Vector3(0, 0, secondaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
                }
            }
        }
    }
}
