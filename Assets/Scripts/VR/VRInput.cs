using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_2017_2_OR_NEWER
using InputTracking = UnityEngine.XR.InputTracking;
using Node = UnityEngine.XR.XRNode;
#else
using InputTracking = UnityEngine.VR.InputTracking;
using Node = UnityEngine.VR.VRNode;
#endif

namespace VRtist
{
    using InputPair = KeyValuePair<InputDevice, InputFeatureUsage<bool>>;


    class ControllerValues
    {
        public bool     primary2DAxisClickState;
        public bool     triggerButtonPressed;
        public float    triggerValue;
        public float    gripValue;
        public bool     gripButtonPressed;
        public bool     primaryButtonState;
        public bool     secondaryButtonState;
        public Vector2  primary2DAxis;
    }

    class VRInput
    {

        public static float deadZoneIn = 0.3f; // dead zone on press analogic controller buttons (trigger & grip)
        public static float deadZoneDeltaOut = 0.2f; // delta of release button between 2 frames before sending release event

        static List<InputDevice> inputDevices = new List<InputDevice>();
        public static InputDevice head;
        public static InputDevice leftController;
        public static InputDevice rightController;

        public static ControllerValues leftControllerValues = new ControllerValues();
        public static ControllerValues rightControllerValues = new ControllerValues();

        static Dictionary<InputDevice, ControllerValues> currentControllerValues = new Dictionary<InputDevice, ControllerValues>();
        static Dictionary<InputDevice, ControllerValues> prevControllerValues = new Dictionary<InputDevice, ControllerValues>();

        // ensure release is called only if press was executed (even if onPress is null)
        static HashSet<InputPair> wasPressed = new HashSet<InputPair>();

        public static void UpdateControllerValues()
        {
            BackupControllerValues();
            FillCurrentControllerValues();
        }

        // copy current frame controller values to previous frame
        public static void BackupControllerValues()
        {
            prevControllerValues.Clear();
            foreach(KeyValuePair<InputDevice, ControllerValues> controllerValues in currentControllerValues)
            {
                ControllerValues values = new ControllerValues();
                values.primary2DAxisClickState = controllerValues.Value.primary2DAxisClickState;
                values.triggerButtonPressed = controllerValues.Value.triggerButtonPressed;
                values.triggerValue = controllerValues.Value.triggerValue;
                values.gripValue = controllerValues.Value.gripValue;
                values.gripButtonPressed = controllerValues.Value.gripButtonPressed;
                values.primaryButtonState = controllerValues.Value.primaryButtonState;
                values.secondaryButtonState = controllerValues.Value.secondaryButtonState;
                values.primary2DAxis = controllerValues.Value.primary2DAxis;

                prevControllerValues[controllerValues.Key] = values;
            }
        }

        // get values from controllers and store them
        public static void FillCurrentControllerValues()
        { 
            List<InputDevice> devices = new List<InputDevice>() { leftController, rightController };
            foreach (InputDevice device in devices)
            {
                bool bValue;
                device.TryGetFeatureValue(CommonUsages.primaryButton, out bValue);
                UpdateControllerValue(device, CommonUsages.primaryButton, bValue);

                device.TryGetFeatureValue(CommonUsages.secondaryButton, out bValue);
                UpdateControllerValue(device, CommonUsages.secondaryButton, bValue);

                device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bValue);
                UpdateControllerValue(device, CommonUsages.primary2DAxisClick, bValue);

                float fValue;
                device.TryGetFeatureValue(CommonUsages.trigger, out fValue);
                UpdateControllerValue(device, CommonUsages.trigger, fValue);
                {
                    float prevValue = GetPrevValue(device, CommonUsages.trigger);
                    bool prevButton = GetPrevValue(device, CommonUsages.triggerButton);
                    if (!prevButton && (fValue - prevValue) > 0 && fValue > deadZoneIn)
                        UpdateControllerValue(device, CommonUsages.triggerButton, true);
                    else if (prevButton && ((prevValue - fValue) > deadZoneDeltaOut || fValue <= deadZoneIn))
                        UpdateControllerValue(device, CommonUsages.triggerButton, false);
                    else
                        UpdateControllerValue(device, CommonUsages.triggerButton, prevButton);
                }

                device.TryGetFeatureValue(CommonUsages.grip, out fValue);
                UpdateControllerValue(device, CommonUsages.grip, fValue);
                {
                    float prevValue = GetPrevValue(device, CommonUsages.grip);
                    bool prevButton = GetPrevValue(device, CommonUsages.gripButton );
                    if (!prevButton && (fValue - prevValue) > 0 && fValue > deadZoneIn)
                        UpdateControllerValue(device, CommonUsages.gripButton, true);
                    else if (prevButton && ((prevValue - fValue) > deadZoneDeltaOut || fValue <= deadZoneIn))
                        UpdateControllerValue(device, CommonUsages.gripButton, false);
                    else
                        UpdateControllerValue(device, CommonUsages.gripButton, prevButton);
                }

                Vector2 vValue;
                device.TryGetFeatureValue(CommonUsages.primary2DAxis, out vValue);
                UpdateControllerValue(device, CommonUsages.primary2DAxis, vValue);
            }

        }
        static void UpdateControllerValue(InputDevice controller, InputFeatureUsage<Vector2> usage, Vector2 value)
        {
            ControllerValues controllerValue = currentControllerValues[controller];
            if (usage == CommonUsages.primary2DAxis)
            {
                controllerValue.primary2DAxis = value;
            }
        }

        static void UpdateControllerValue(InputDevice controller, InputFeatureUsage<float> usage, float value)
        {
            ControllerValues controllerValue = currentControllerValues[controller];
            if (usage == CommonUsages.trigger)
            {
                controllerValue.triggerValue = value;
            }
            if (usage == CommonUsages.grip)
            {
                controllerValue.gripValue = value;
            }
        }

        static void UpdateControllerValue(InputDevice controller, InputFeatureUsage<float> usage, bool value)
        {
            ControllerValues controllerValue = currentControllerValues[controller];
            if (usage == CommonUsages.trigger)
            {
                controllerValue.triggerButtonPressed = value;
            }
            if (usage == CommonUsages.grip)
            {
                controllerValue.gripButtonPressed = value;
            }
        }

        static void UpdateControllerValue(InputDevice controller, InputFeatureUsage<bool> usage, bool value)
        {
            ControllerValues controllerValue = currentControllerValues[controller];
            if (usage == CommonUsages.primary2DAxisClick)
            {
                controllerValue.primary2DAxisClickState = value;
            }
            else if (usage == CommonUsages.triggerButton)
            {
                controllerValue.triggerButtonPressed = value;
            }
            else if (usage == CommonUsages.primaryButton)
            {
                controllerValue.primaryButtonState = value;
            }
            else if (usage == CommonUsages.secondaryButton)
            {
                controllerValue.secondaryButtonState = value;
            }
            else if (usage == CommonUsages.gripButton)
            {
                controllerValue.gripButtonPressed = value;
            }
        }
        public static Vector2 GetValue(InputDevice controller, InputFeatureUsage<Vector2> usage)
        {
            return _GetValue(currentControllerValues, controller, usage);
        }
        public static float GetValue(InputDevice controller, InputFeatureUsage<float> usage)
        {
            return _GetValue(currentControllerValues, controller, usage);
        }
        public static bool GetValue(InputDevice controller, InputFeatureUsage<bool> usage)
        {
            return _GetValue(currentControllerValues, controller, usage);
        }
        static Vector2 GetPrevValue(InputDevice controller, InputFeatureUsage<Vector2> usage)
        {
            return _GetValue(prevControllerValues, controller, usage);
        }
        static float GetPrevValue(InputDevice controller, InputFeatureUsage<float> usage)
        {
            return _GetValue(prevControllerValues, controller, usage);
        }
        static bool GetPrevValue(InputDevice controller, InputFeatureUsage<bool> usage)
        {
            return _GetValue(prevControllerValues, controller, usage);
        }

        static Vector2 _GetValue(Dictionary<InputDevice, ControllerValues> controllerValues, InputDevice controller, InputFeatureUsage<Vector2> usage)
        {            
            ControllerValues controllerValue = controllerValues[controller];
            if (usage == CommonUsages.primary2DAxis)
            {
                return controllerValue.primary2DAxis;
            }
            return Vector2.zero;
        }

        static float _GetValue(Dictionary<InputDevice, ControllerValues> controllerValues, InputDevice controller, InputFeatureUsage<float> usage)
        {
            ControllerValues controllerValue = controllerValues[controller];
            if (usage == CommonUsages.trigger)
            {
                return controllerValue.triggerValue;
            }
            else if (usage == CommonUsages.grip)
            {
                return controllerValue.gripValue;
            }
            return 0f;
        }

        static bool _GetValue(Dictionary<InputDevice, ControllerValues> controllerValues, InputDevice controller, InputFeatureUsage<bool> usage)
        {
            ControllerValues controllerValue = controllerValues[controller];
            if (usage == CommonUsages.primary2DAxisClick)
            {
                return controllerValue.primary2DAxisClickState;
            }
            else if (usage == CommonUsages.triggerButton)
            {
                return controllerValue.triggerButtonPressed;
            }
            else if (usage == CommonUsages.primaryButton)
            {
                return controllerValue.primaryButtonState;
            }
            else if (usage == CommonUsages.secondaryButton)
            {
                return controllerValue.secondaryButtonState;
            }
            else if (usage == CommonUsages.gripButton)
            {
                return controllerValue.gripButtonPressed;
            }
            return false;
        }

        public static void ButtonEvent(InputDevice controller, InputFeatureUsage<float> usage, System.Action onPress = null, System.Action onRelease = null)
        {
            ButtonEvent(controller, usage == CommonUsages.trigger ? CommonUsages.triggerButton : CommonUsages.gripButton, onPress, onRelease);
        }

        public static void ButtonEvent(InputDevice controller, InputFeatureUsage<bool> usage, System.Action onPress = null, System.Action onRelease = null)
        {
            bool prevButtonPressed = GetPrevValue(controller, usage);
            bool currentButtonPressed = GetValue(controller, usage);
            if (prevButtonPressed != currentButtonPressed)
            {
                if (currentButtonPressed)
                {
                    wasPressed.Add(new InputPair(controller, usage));
                    if (onPress != null)
                    {                        
                        onPress();
                    }                
                }
                else
                {
                    InputPair pair = new InputPair(controller, usage);
                    if (wasPressed.Contains(pair) && onRelease != null)
                    {
                        onRelease();
                    }
                    wasPressed.Remove(pair);
                }
            }
        }
        public static bool TryGetDevices()
        {
            if (!head.isValid || !leftController.isValid || !rightController.isValid)
            {
                var inputDevices = new List<InputDevice>();
                InputDevices.GetDevices(inputDevices);
                foreach (var device in inputDevices)
                {
                    if (device.characteristics == (InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice)) { head = device; }
                    if (device.characteristics == (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice)) { rightController = device; }
                    if (device.characteristics == (InputDeviceCharacteristics.Left | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice)) { leftController = device; }
                    Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.characteristics.ToString()));
                }
                if (!head.isValid) { Debug.LogWarning("Generic device not found !!"); }
                if (!leftController.isValid) { Debug.LogWarning("Left device not found !!"); }
                else
                {
                    currentControllerValues[leftController] = leftControllerValues;
                    prevControllerValues[leftController] = leftControllerValues;
                }
                if (!rightController.isValid) { Debug.LogWarning("Right device not found !!"); }
                else
                {
                    currentControllerValues[rightController] = rightControllerValues;
                    prevControllerValues[rightController] = rightControllerValues;
                }
                if (currentControllerValues.Count == 2)
                {
                    FillCurrentControllerValues();
                    UpdateControllerValues();
                }
            }
            return head.isValid && leftController.isValid && rightController.isValid;
        }

        class DeviceTransform
        {
            public Quaternion rotation;
            public Vector3 position;
        }
        static Dictionary<InputDevice, DeviceTransform> prevDeviceTransform = new Dictionary<InputDevice, DeviceTransform>();

        public static void GetControllerTransform(InputDevice controller, out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (!controller.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                Debug.Log("Error getting device rotation");
            }
            position = Vector3.zero;
            if (!controller.TryGetFeatureValue(CommonUsages.devicePosition, out position))
            {
                Debug.Log("Error getting device position");
            }

            // OpenVR offsets
#if false
            if (OVRManager.loadedXRDevice == OVRManager.XRDevice.OpenVR)
            {
                Node node = controller.role == InputDeviceRole.RightHanded ? Node.RightHand : Node.LeftHand;
                OVRPose pose = OVRManager.GetOpenVRControllerOffset(node);
                rotation *= pose.orientation;
                position += pose.position;
            }
#endif
            // Filter left and right controllers
            if (!prevDeviceTransform.ContainsKey(controller))
                prevDeviceTransform[controller] = new DeviceTransform();

            DeviceTransform prevTransform = prevDeviceTransform[controller];
            rotation = Quaternion.Slerp(prevTransform.rotation, rotation, 0.3f);
            position = Vector3.Lerp(prevTransform.position, position, 0.3f);
            prevTransform.rotation = rotation;
            prevTransform.position = position;
        }

        public static void UpdateTransformFromVRDevice(Transform transform, InputDevice device, out Vector3 position, out Quaternion rotation)
        {
            GetControllerTransform(device, out position, out rotation);
            transform.localPosition = position;
            transform.localRotation = rotation;
        }

        public static void DeepSetLayer(GameObject gameObject, int layer)
        {
            if (gameObject == null) { return; }
            foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
            }

        }
    }
}