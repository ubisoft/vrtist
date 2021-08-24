using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class AnimateControllerOculus : AnimateControllerAbstract
    {

        private float gripRotationAmplitude = 15f;
        private float triggerRotationAmplitude = 15f;
        private float joystickRotationAmplitude = 15f;
        private float primaryTranslationAmplitude = -0.0016f;
        private float secondaryTranslationAmplitude = -0.0016f;

        protected override void AnimateGrip(float gripAmount)
        {
            gripTransform.localRotation = initGripRotation * Quaternion.Euler(0, gripAmount * gripRotationAmplitude * -(int)gripDirection, 0);
            gripTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gripAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
        }

        protected override void AnimateJoystick(Vector2 joystick)
        {
            joystickTransform.localRotation = initJoystickRotation * Quaternion.Euler(joystick.y * joystickRotationAmplitude, 0, -joystick.x * joystickRotationAmplitude);
            joystickTransform.gameObject.GetComponentInChildren<MeshRenderer>().materials[1].SetColor("_BaseColor", joystick.magnitude > 0.05f ? UIOptions.SelectedColor : Color.black);
        }

        protected override void AnimatePrimaryButton(bool primaryState)
        {
            primaryTransform.localPosition = initPrimaryTranslation;
            primaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", primaryState ? UIOptions.SelectedColor : Color.black);
            if (primaryState)
            {
                primaryTransform.localPosition += new Vector3(0, 0, primaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
            }
        }

        protected override void AnimateSecondaryButton(bool secondaryState)
        {
            secondaryTransform.localPosition = initSecondaryTranslation;
            secondaryTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", secondaryState ? UIOptions.SelectedColor : Color.black);
            if (secondaryState)
            {
                secondaryTransform.localPosition += new Vector3(0, 0, secondaryTranslationAmplitude); // TODO: quick anim? CoRoutine.
            }
        }

        protected override void AnimateTrigger(float triggerAmount)
        {
            triggerTransform.localRotation = initTriggerRotation * Quaternion.Euler(triggerAmount * triggerRotationAmplitude, 0, 0);
            triggerTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", triggerAmount > 0.01f ? UIOptions.SelectedColor : Color.black);
        }
    }

}