﻿/* MIT License
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

#if UNITY_2017_2_OR_NEWER
#else
using InputTracking = UnityEngine.VR.InputTracking;
using Node = UnityEngine.VR.VRNode;
#endif

namespace VRtist
{
    using InputPair = KeyValuePair<InputDevice, InputFeatureUsage<bool>>;
    using JoyInputPair = KeyValuePair<InputDevice, VRInput.JoyDirection>;

    class ControllerValues
    {
        public bool primary2DAxisClickState;
        public bool triggerButtonPressed;
        public float triggerValue;
        public float gripValue;
        public bool gripButtonPressed;
        public bool primaryButtonState;
        public bool secondaryButtonState;
        public Vector2 primary2DAxis;
    }

    public enum VRDevice
    {
        Head,
        PrimaryController,
        SecondaryController
    }

    class VRInput
    {

        public static float deadZoneIn = 0.3f; // dead zone on press analogic controller buttons (trigger & grip)
        public static float deadZoneDeltaOut = 0.2f; // delta of release button between 2 frames before sending release event

        static List<InputDevice> inputDevices = new List<InputDevice>();
        public static InputDevice head;
        public static InputDevice secondaryController;
        public static InputDevice primaryController;
        private static bool remapLeftRightHandedDevices = true;
        public static Dictionary<InputDevice, InputDevice> invertedController = new Dictionary<InputDevice, InputDevice>();

        public static ControllerValues secondaryControllerValues = new ControllerValues();
        public static ControllerValues primaryControllerValues = new ControllerValues();

        static Dictionary<InputDevice, ControllerValues> currentControllerValues = new Dictionary<InputDevice, ControllerValues>();
        static Dictionary<InputDevice, ControllerValues> prevControllerValues = new Dictionary<InputDevice, ControllerValues>();

        static HashSet<InputPair> justPressed = new HashSet<InputPair>();
        static HashSet<InputPair> justReleased = new HashSet<InputPair>();

        public enum JoyDirection { UP, DOWN, LEFT, RIGHT };
        static HashSet<JoyInputPair> joyJustPressed = new HashSet<JoyInputPair>();
        static HashSet<JoyInputPair> joyJustReleased = new HashSet<JoyInputPair>();
        static HashSet<JoyInputPair> joyLongPush = new HashSet<JoyInputPair>();

        static float longPushTimer = 0.0f;

        public static void UpdateControllerValues()
        {
            BackupControllerValues();
            FillCurrentControllerValues();
        }

        // copy current frame controller values to previous frame
        public static void BackupControllerValues()
        {
            prevControllerValues.Clear();
            foreach (KeyValuePair<InputDevice, ControllerValues> controllerValues in currentControllerValues)
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
            joyJustPressed.Clear();
            joyJustReleased.Clear();

            List<InputDevice> devices = new List<InputDevice>() { secondaryController, primaryController };
            foreach (InputDevice device in devices)
            {
                bool bValue;
                device.TryGetFeatureValue(CommonUsages.primaryButton, out bValue);
                UpdateControllerValue(device, CommonUsages.primaryButton, bValue);
                UpdateControllerDelta(device, CommonUsages.primaryButton);

                device.TryGetFeatureValue(CommonUsages.secondaryButton, out bValue);
                UpdateControllerValue(device, CommonUsages.secondaryButton, bValue);
                UpdateControllerDelta(device, CommonUsages.secondaryButton);

                device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bValue);
                UpdateControllerValue(device, CommonUsages.primary2DAxisClick, bValue);
                UpdateControllerDelta(device, CommonUsages.primary2DAxisClick);

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

                    UpdateControllerDelta(device, CommonUsages.triggerButton);
                }

                device.TryGetFeatureValue(CommonUsages.grip, out fValue);
                UpdateControllerValue(device, CommonUsages.grip, fValue);
                {
                    float prevValue = GetPrevValue(device, CommonUsages.grip);
                    bool prevButton = GetPrevValue(device, CommonUsages.gripButton);
                    if (!prevButton && (fValue - prevValue) > 0 && fValue > deadZoneIn)
                        UpdateControllerValue(device, CommonUsages.gripButton, true);
                    else if (prevButton && ((prevValue - fValue) > deadZoneDeltaOut || fValue <= deadZoneIn))
                        UpdateControllerValue(device, CommonUsages.gripButton, false);
                    else
                        UpdateControllerValue(device, CommonUsages.gripButton, prevButton);

                    UpdateControllerDelta(device, CommonUsages.gripButton);
                }

                Vector2 vValue;
                device.TryGetFeatureValue(CommonUsages.primary2DAxis, out vValue);
                UpdateControllerValue(device, CommonUsages.primary2DAxis, vValue);
                UpdateControllerDelta(device, CommonUsages.primary2DAxis);
            }
        }

        public static JoyDirection GetQuadrant(Vector2 value)
        {
            if (value.x > 0.0f && value.x > value.y)
            {
                return JoyDirection.RIGHT;
            }

            if (value.y > 0.0f && value.y > value.x)
            {
                return JoyDirection.UP;
            }

            if (value.x <= 0.0f && value.x < value.y)
            {
                return JoyDirection.LEFT;
            }

            if (value.y <= 0.0f && value.y < value.x)
            {
                return JoyDirection.DOWN;
            }

            return JoyDirection.DOWN; // default?
        }

        static void ClearLongPush(InputDevice controller)
        {
            HashSet<JoyInputPair> longPush = new HashSet<JoyInputPair>();
            foreach (JoyInputPair joyInput in joyLongPush)
            {
                if (joyInput.Key != controller)
                    longPush.Add(joyInput);
            }
            joyLongPush = longPush;
        }

        static void UpdateControllerDelta(InputDevice controller, InputFeatureUsage<Vector2> usage)
        {
            Vector2 v2PrevValue;
            Vector2 v2CurrValue;
            remapLeftRightHandedDevices = false;
            v2PrevValue = GetPrevValue(controller, usage);
            v2CurrValue = GetValue(controller, usage);
            remapLeftRightHandedDevices = true;

            float prevLen = v2PrevValue.magnitude;
            float currLen = v2CurrValue.magnitude;

            JoyDirection prevQuadrant = GetQuadrant(v2PrevValue);
            JoyDirection currQuadrant = GetQuadrant(v2CurrValue);

            JoyInputPair prevPair = new JoyInputPair(controller, prevQuadrant);
            JoyInputPair currPair = new JoyInputPair(controller, currQuadrant);

            if (currLen > deadZoneIn)
            {
                if (prevLen <= deadZoneIn)
                {
                    // justPressed center -> exterior
                    joyJustPressed.Add(currPair);
                    longPushTimer = 0.0f;
                }
                else
                {
                    // still in EXT zone
                    if (prevQuadrant == currQuadrant)
                    {
                        longPushTimer += Time.unscaledDeltaTime;
                        if (longPushTimer > 0.6f && !joyLongPush.Contains(currPair)) // TODO: put timer threshold in GlobalState.Settings
                        {
                            joyLongPush.Add(currPair);
                        }
                    }
                    else
                    {
                        // quadrant changed
                        joyJustPressed.Add(currPair);
                        joyJustReleased.Add(prevPair);
                        longPushTimer = 0.0f;
                        ClearLongPush(controller);
                    }
                }
            }
            else
            {
                if (prevLen > deadZoneIn)
                {
                    // justReleased EXT -> Center
                    joyJustReleased.Add(currPair);
                    ClearLongPush(controller);

                }
                else
                {
                    // still in center
                    ClearLongPush(controller);
                }
            }
        }

        static void UpdateControllerDelta(InputDevice controller, InputFeatureUsage<bool> usage)
        {
            bool bPrevValue;
            bool bCurrValue;
            remapLeftRightHandedDevices = false;
            bPrevValue = GetPrevValue(controller, usage);
            bCurrValue = GetValue(controller, usage);
            remapLeftRightHandedDevices = true;
            InputPair pair = new InputPair(controller, usage);
            if (bPrevValue == bCurrValue)
            {
                justPressed.Remove(pair);
                justReleased.Remove(pair);
            }
            else
            {
                if (bCurrValue)
                {
                    justPressed.Add(pair);
                    justReleased.Remove(pair);
                }
                else
                {
                    justReleased.Add(pair);
                    justPressed.Remove(pair);
                }
            }
        }

        static void UpdateControllerValue(InputDevice controller, InputFeatureUsage<Vector2> usage, Vector2 value)
        {
            if (!currentControllerValues.ContainsKey(controller)) { return; }

            ControllerValues controllerValue = currentControllerValues[controller];
            if (usage == CommonUsages.primary2DAxis)
            {
                controllerValue.primary2DAxis = value;
            }
        }

        static void UpdateControllerValue(InputDevice controller, InputFeatureUsage<float> usage, float value)
        {
            if (!currentControllerValues.ContainsKey(controller)) { return; }

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
            if (!currentControllerValues.ContainsKey(controller)) { return; }

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
            if (!currentControllerValues.ContainsKey(controller)) { return; }

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

        private static InputDevice GetLeftOrRightHandedController(InputDevice controller)
        {
            if (GlobalState.Settings.rightHanded || !remapLeftRightHandedDevices)
                return controller;
            if (invertedController.Count == 0) { return new InputDevice(); }  // invalid and unknown in our maps of devices
            return invertedController[controller];
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
            InputDevice c = GetLeftOrRightHandedController(controller);
            if (!controllerValues.ContainsKey(c)) { return Vector2.zero; }

            ControllerValues controllerValue = controllerValues[c];
            if (usage == CommonUsages.primary2DAxis)
            {
                Vector2 value = controllerValue.primary2DAxis;
                Vector2 absvalue = new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
                if (absvalue.x >= deadZoneIn)
                {
                    value.x = Mathf.Sign(value.x) * (absvalue.x - deadZoneIn) * (1f / (1f - deadZoneIn));
                }
                else
                {
                    value.x = 0;
                }
                if (absvalue.y >= deadZoneIn)
                {
                    value.y = Mathf.Sign(value.y) * (absvalue.y - deadZoneIn) * (1f / (1f - deadZoneIn));
                }
                else
                {
                    value.y = 0;
                }
                return value;
                //return controllerValue.primary2DAxis;
            }
            return Vector2.zero;
        }

        static float _GetValue(Dictionary<InputDevice, ControllerValues> controllerValues, InputDevice controller, InputFeatureUsage<float> usage)
        {
            InputDevice c = GetLeftOrRightHandedController(controller);
            if (!controllerValues.ContainsKey(c)) { return 0f; }

            ControllerValues controllerValue = controllerValues[c];
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
            InputDevice c = GetLeftOrRightHandedController(controller);
            if (!controllerValues.ContainsKey(c)) { return false; }

            ControllerValues controllerValue = controllerValues[c];
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
            InputDevice c = GetLeftOrRightHandedController(controller);
            InputPair pair = new InputPair(c, usage);
            if (onPress != null && justPressed.Contains(pair))
            {
                onPress();
            }

            if (onRelease != null && justReleased.Contains(pair))
            {
                onRelease();
            }
        }

        public static void GetInstantButtonEvent(InputDevice controller, InputFeatureUsage<bool> usage, ref bool outJustPressed, ref bool outJustReleased)
        {
            InputDevice c = GetLeftOrRightHandedController(controller);
            InputPair pair = new InputPair(c, usage);
            outJustPressed = justPressed.Contains(pair);
            outJustReleased = justReleased.Contains(pair);
        }

        public static void GetInstantJoyEvent(InputDevice controller, JoyDirection direction, ref bool outJustPressed, ref bool outJustReleased, ref bool outLongPush)
        {
            InputDevice c = GetLeftOrRightHandedController(controller);
            JoyInputPair pair = new JoyInputPair(c, direction);
            outJustPressed = joyJustPressed.Contains(pair);
            outJustReleased = joyJustReleased.Contains(pair);
            outLongPush = joyLongPush.Contains(pair);
        }

        public static void InitInvertedControllers()
        {
            if (secondaryController.isValid && primaryController.isValid)
            {
                invertedController[head] = head;
                invertedController[secondaryController] = primaryController;
                invertedController[primaryController] = secondaryController;
                Debug.Log("Got left/right handed controllers");
            }
        }

        public static bool TryGetDevices()
        {
            if (!head.isValid || !secondaryController.isValid || !primaryController.isValid)
            {
                var inputDevices = new List<InputDevice>();
                InputDevices.GetDevices(inputDevices);
                foreach (var device in inputDevices)
                {
                    if (device.characteristics == (InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice)) { head = device; }
                    if (device.characteristics == (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice)) { primaryController = device; }
                    if (device.characteristics == (InputDeviceCharacteristics.Left | InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice)) { secondaryController = device; }
                    Debug.Log(string.Format("Device found with name '{0}' and role '{1}'", device.name, device.characteristics.ToString()));
                }
                if (!head.isValid) { Debug.LogWarning("Generic device not found !!"); }
                if (!secondaryController.isValid) { Debug.LogWarning("Left device not found !!"); }
                else
                {
                    currentControllerValues[secondaryController] = secondaryControllerValues;
                    prevControllerValues[secondaryController] = secondaryControllerValues;
                }
                if (!primaryController.isValid) { Debug.LogWarning("Right device not found !!"); }
                else
                {
                    currentControllerValues[primaryController] = primaryControllerValues;
                    prevControllerValues[primaryController] = primaryControllerValues;
                }
                if (currentControllerValues.Count == 2)
                {
                    GlobalState.Instance.VRControllers.InitializeControllers(primaryController.name);
                    InitInvertedControllers();
                    FillCurrentControllerValues();
                    UpdateControllerValues();
                }
            }

            return head.isValid && secondaryController.isValid && primaryController.isValid;
        }

        

        class DeviceTransform
        {
            public Quaternion rotation = Quaternion.identity;
            public Vector3 position = Vector3.zero;
        }
        static Dictionary<InputDevice, DeviceTransform> prevDeviceTransform = new Dictionary<InputDevice, DeviceTransform>();

        public static void GetControllerTransform(InputDevice controller, out Vector3 position, out Quaternion rotation)
        {
            InputDevice c = GetLeftOrRightHandedController(controller);

            rotation = Quaternion.identity;
            if (!c.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                Debug.Log("Error getting device rotation");
            }
            position = Vector3.zero;
            if (!c.TryGetFeatureValue(CommonUsages.devicePosition, out position))
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
            if (!prevDeviceTransform.ContainsKey(c))
                prevDeviceTransform[c] = new DeviceTransform();

            DeviceTransform prevTransform = prevDeviceTransform[c];
            rotation = Quaternion.Slerp(prevTransform.rotation, rotation, 0.3f);
            position = Vector3.Lerp(prevTransform.position, position, 0.3f);
            prevTransform.rotation = rotation;
            prevTransform.position = position;
        }

        public static void UpdateTransformFromVRDevice(Transform transform, InputDevice device)
        {
            GetControllerTransform(device, out Vector3 position, out Quaternion rotation);
            transform.localPosition = position;
            transform.localRotation = rotation;
        }

        public static void DeepSetLayer(GameObject gameObject, string layerName)
        {
            if (gameObject == null) { return; }
            int layer = LayerMask.NameToLayer(layerName);
            foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
            }

        }

        // duration is seconds (only on oculus).
        // amplitude in [0..1]
        public static void SendHaptic(InputDevice controller, float duration, float amplitude = 1f)
        {
            InputDevice c = GetLeftOrRightHandedController(controller);
            c.SendHapticImpulse(0, amplitude, duration);
        }

        public static void SendHapticImpulse(InputDevice controller, uint channel, float amplitude, float duration = 1)
        {
            InputDevice c = GetLeftOrRightHandedController(controller);
            c.SendHapticImpulse(channel, amplitude, duration);
        }
    }
}