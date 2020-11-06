using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AnimateControllerButtons : MonoBehaviour
    {
        public Transform gripTransform = null;
        public float gripRotationAmplitude = 15.0f;
        private Quaternion initGripRotation = Quaternion.identity;

        public Transform triggerTransform = null;
        public float triggerRotationAmplitude = 15.0f;
        private Quaternion initTriggerRotation = Quaternion.identity;

        public Transform joystickTransform = null;
        public float joystickRotationAmplitude = 15.0f;
        private Quaternion initJoystickRotation = Quaternion.identity;

        public Transform primaryTransform = null;
        public float primaryTranslationAmplitude = -0.0016f;
        private Vector3 initPrimaryTranslation = Vector3.zero;

        public Transform secondaryTransform = null;
        public float secondaryTranslationAmplitude = -0.0016f;
        private Vector3 initSecondaryTranslation = Vector3.zero;

        public Transform systemTransform = null;
        public float systemTranslationAmplitude = -0.001f;
        private Vector3 initSystemTranslation = Vector3.zero;

        private InputDevice device;
        private Transform controllerTransform = null;

        // Start is called before the first frame update
        void Start()
        {
            CaptureControllers();
            CaptureInitialTransforms();
        }

        private void CaptureControllers()
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                string childName = transform.GetChild(i).gameObject.name;
                if (childName == "left_controller")
                {
                    device = VRInput.leftController;
                    controllerTransform = transform.GetChild(i);
                    break;
                }
                else if (childName == "right_controller")
                {
                    device = VRInput.rightController;
                    controllerTransform = transform.GetChild(i);
                    break;
                }
            }

            if (!device.isValid || controllerTransform == null)
            {
                Debug.LogError("AnimateControllerButtons could not find the controller.");
            }
        }

        private void CaptureInitialTransforms()
        {
            if (null != controllerTransform)
            {
                gripTransform = controllerTransform.Find("GripButtonPivot/GripButton");
                if (null != gripTransform)
                {
                    initGripRotation = gripTransform.localRotation;
                }

                triggerTransform = controllerTransform.Find("TriggerButtonPivot/TriggerButton");
                if (null != triggerTransform)
                {
                    initTriggerRotation = triggerTransform.localRotation;
                }

                joystickTransform = controllerTransform.Find("JoystickPivot/Joystick");
                if (null != joystickTransform)
                {
                    initJoystickRotation = joystickTransform.localRotation;
                    // FIX: for an unknown reason, the jostick obect is disabled at start.
                    joystickTransform.gameObject.SetActive(true);
                }

                primaryTransform = controllerTransform.Find("PrimaryButtonPivot/PrimaryButton");
                if (null != primaryTransform)
                {
                    initPrimaryTranslation = primaryTransform.localPosition;
                }

                secondaryTransform = controllerTransform.Find("SecondaryButtonPivot/SecondaryButton");
                if (null != secondaryTransform)
                {
                    initSecondaryTranslation = secondaryTransform.localPosition;
                }

                systemTransform = controllerTransform.Find("SystemButtonPivot/SystemButton");
                if (null != systemTransform)
                {
                    initSystemTranslation = systemTransform.localPosition;
                }
            }
        }

        public void OnRightHanded()
        {
            // TODO: handle what needs to be handled when we change hands.

            CaptureControllers();
            CaptureInitialTransforms();
        }

        // Update is called once per frame
        void Update()
        {
            if (!device.isValid || controllerTransform == null)
            {
                CaptureControllers();
                CaptureInitialTransforms();
            }

            // GRIP
            if (null != gripTransform)
            {
                float gripAmount = VRInput.GetValue(device, CommonUsages.grip);
                gripTransform.localRotation = initGripRotation * Quaternion.Euler(0, gripAmount * gripRotationAmplitude, 0);
                gripTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gripAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
            }

            // TRIGGER
            if (null != triggerTransform)
            {
                float triggerAmount = VRInput.GetValue(device, CommonUsages.trigger);
                triggerTransform.localRotation = initTriggerRotation * Quaternion.Euler(triggerAmount * gripRotationAmplitude, 0, 0);
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

            // SYSTEM
            if (null != systemTransform)
            {
                ////bool systemState = VRInput.GetValue(device, CommonUsages.menuButton);
                ////systemTransform.localPosition = initSystemTranslation;
                ////systemTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", systemState ? UIOptions.SelectedColor : Color.black);
                ////if (systemState)
                ////{
                ////    systemTransform.localPosition += new Vector3(0, 0, systemTranslationAmplitude); // TODO: quick anim? CoRoutine.
                ////}
            }

        }
    }
}
