using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class CameraController : MonoBehaviour
    {
        [Header("Base Parameters")]
        public Transform world = null;
        public float playerSpeed = 0.2f;
        public Transform leftHandle = null;

        Vector3 initControllerPosition;
        Quaternion initControllerRotation;
        Matrix4x4 initControllerMatrix;
        Matrix4x4 initWorldMatrix;

        const float deadZone = 0.3f;
        const float scaleFactor = 1.05f;
        float scale;

        Vector3 initCameraPosition;
        Quaternion initCameraRotation;

        bool rotating = false;

        // Start is called before the first frame update
        void Start()
        {
            VRInput.TryGetDevices();

            leftHandle = transform.Find("Pivot/LeftHandle");
            if (leftHandle == null) { Debug.LogWarning("Cannot find 'LeftHandle' game object"); }

            initCameraPosition = transform.position;
            initCameraRotation = transform.rotation;
        }

        // Update is called once per frame
        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                UpdateNavigation();

                VRInput.ButtonEvent(VRInput.leftController, CommonUsages.primaryButton, () => { },
                () =>
                {
                    CommandManager.Undo();
                });
                VRInput.ButtonEvent(VRInput.leftController, CommonUsages.secondaryButton, () => { },
                () =>
                {
                    CommandManager.Redo();
                });
            }

        }

        private void UpdateNavigation()
        {
            // Left controller == Navigation
            if (!leftHandle.gameObject.activeSelf) { leftHandle.gameObject.SetActive(true); }

            // Update controller transform
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.leftController, out position, out rotation);
            leftHandle.localPosition = position;
            leftHandle.localRotation = rotation;           

            // Move & rotate
            Vector2 val = Vector2.zero;
            if (VRInput.leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out val) && val != Vector2.zero)
            {                
                float d = Vector3.Distance(world.transform.TransformPoint(Vector3.one), world.transform.TransformPoint(Vector3.zero));

                Vector3 velocity = Camera.main.transform.forward * val.y * d;
                transform.position += velocity * playerSpeed;

                if (Mathf.Abs(val.x) > 0.95f && !rotating)
                {
                    transform.rotation *= Quaternion.Euler(0f, Mathf.Sign(val.x) * 45f, 0f);
                    rotating = true;
                }
                if (Mathf.Abs(val.x) <= 0.95f && rotating)
                {
                    rotating = false;
                }
            }

            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.primary2DAxisClick,
            () =>
            {
                world.localPosition = Vector3.zero;
                world.localRotation = Quaternion.identity;
                world.localScale = Vector3.one;

                transform.position = initCameraPosition;
                transform.rotation = initCameraRotation;
            });

            // grip world
            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.gripButton,
            () =>
            {
                VRInput.GetControllerTransform(VRInput.leftController, out initControllerPosition, out initControllerRotation);
                Transform parent = leftHandle.parent;

                initControllerMatrix = (parent.localToWorldMatrix * Matrix4x4.TRS(initControllerPosition, initControllerRotation, Vector3.one)).inverse;
                initWorldMatrix = world.localToWorldMatrix;
                scale = 1f;

                leftHandle.localScale = Vector3.zero;
            },
            () => {
                leftHandle.localScale = Vector3.one;
            });

            if (VRInput.GetValue(VRInput.leftController, CommonUsages.grip) > deadZone)
            {
                Transform parent = leftHandle.parent;

                Vector2 joystickAxis = VRInput.GetValue(VRInput.leftController, CommonUsages.primary2DAxis);
                float prevScale = scale;
                if (joystickAxis.y > deadZone)
                    scale *= scaleFactor;
                if (joystickAxis.y < -deadZone)
                    scale /= scaleFactor;

                Vector3 p;
                Quaternion r;
                VRInput.GetControllerTransform(VRInput.leftController, out p, out r);

                Matrix4x4 controllerMatrix = parent.localToWorldMatrix * Matrix4x4.TRS(p, r, new Vector3(scale, scale, scale)) * initControllerMatrix;

                Matrix4x4 transformed = controllerMatrix * initWorldMatrix;
                world.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                world.localRotation = transformed.rotation;

                float clampedScale = Mathf.Clamp(transformed.lossyScale.x, 0.0005f, 50f);
                world.localScale = new Vector3(clampedScale, clampedScale, clampedScale);
                if (transformed.lossyScale.x != clampedScale)
                {
                    scale = prevScale;
                }

                Camera.main.nearClipPlane = 0.1f * world.localScale.x;
                Camera.main.farClipPlane = 5000f * world.localScale.x;
            }
        }
    }
}