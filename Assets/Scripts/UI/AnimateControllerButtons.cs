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

        // Start is called before the first frame update
        void Start()
        {
            if (gripTransform)
            {
                initGripRotation = gripTransform.localRotation;
            }

            if (triggerTransform)
            {
                initTriggerRotation = triggerTransform.localRotation;
            }

            if (joystickTransform)
            {
                initJoystickRotation = joystickTransform.localRotation;
            }

            if (primaryTransform)
            {
                initPrimaryTranslation = primaryTransform.localPosition;
            }

            if (secondaryTransform)
            {
                initSecondaryTranslation = secondaryTransform.localPosition;
            }

            if (systemTransform)
            {
                initSystemTranslation = systemTransform.localPosition;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // GRIP
            float gripAmount = VRInput.GetValue(VRInput.leftController, CommonUsages.grip);
            gripTransform.localRotation = initGripRotation * Quaternion.Euler(0,gripAmount * gripRotationAmplitude, 0);
            gripTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gripAmount > 0.01f ? UIOptions.SelectedColor : Color.black);

            // TRIGGER
            float triggerAmount = VRInput.GetValue(VRInput.leftController, CommonUsages.trigger);
            triggerTransform.localRotation = initTriggerRotation * Quaternion.Euler(triggerAmount * gripRotationAmplitude, 0, 0);
            triggerTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", triggerAmount > 0.01f ? UIOptions.SelectedColor : Color.black);

            // JOYSTICK
            Vector2 joystick = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
            joystickTransform.localRotation = initJoystickRotation * Quaternion.Euler(joystick.y * joystickRotationAmplitude, 0, joystick.x * -joystickRotationAmplitude);
            joystickTransform.gameObject.GetComponent<MeshRenderer>().materials[1].SetColor("_BaseColor", joystick.magnitude > 0.05f ? UIOptions.SelectedColor : Color.black);

            // PRIMARY
            bool primaryState = VRInput.GetValue(VRInput.leftController, CommonUsages.primaryButton);
            primaryTransform.localPosition = initPrimaryTranslation;
            primaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", primaryState ? UIOptions.SelectedColor : Color.black);
            if (primaryState)
            {
                primaryTransform.localPosition += new Vector3(0, 0, primaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
            }

            // SECONDARY
            bool secondaryState = VRInput.GetValue(VRInput.leftController, CommonUsages.secondaryButton);
            secondaryTransform.localPosition = initSecondaryTranslation;
            secondaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", secondaryState ? UIOptions.SelectedColor : Color.black);
            if (secondaryState)
            {
                secondaryTransform.localPosition += new Vector3(0, 0, secondaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
            }

            // SYSTEM
            ////bool systemState = VRInput.GetValue(VRInput.leftController, CommonUsages.menuButton);
            ////systemTransform.localPosition = initSystemTranslation;
            ////systemTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", systemState ? UIOptions.SelectedColor : Color.black);
            ////if (systemState)
            ////{
            ////    systemTransform.localPosition += new Vector3(0, 0, systemTranslationAmplitude); // TODO: quick anim? CoRoutine.
            ////}

        }
    }
}
