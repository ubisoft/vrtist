using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GlobalState : MonoBehaviour
    {
        [Header("Parameters")]
        public GameObject leftController = null;

        // FPS
        public static bool showFps = false;
        public static int fps { get; private set; }
        public static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        private GameObject displayTooltip = null;

        // World
        public static float worldScale = 1f;
        private static bool isGrippingWorld = false;
        public BoolChangedEvent onGripWorldEvent = new BoolChangedEvent(); // Event for Grip preemption.
        public static bool IsGrippingWorld { get { return isGrippingWorld; } set { isGrippingWorld = value; Instance.onGripWorldEvent.Invoke(value); } }

        // Navigation
        public static NavigationMode currentNavigationMode = null;
        public static float flightSpeed = 5f;
        public static float flightRotationSpeed = 5f;
        public static float flightDamping = 5f;

        // Animation
        public static int startFrame = 1;
        public static int endFrame = 250;
        public static int currentFrame = 1;

        // Gizmos
        public static bool displayGizmos = true;

        private static GlobalState instance = null;
        private static GlobalState Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<GlobalState>();
                }
                return instance;
            }
        }

        void Awake()
        {
            _ = Instance;
        }

        private void Start() {
            if(null != leftController) {
                displayTooltip = Tooltips.CreateTooltip(leftController, Tooltips.Anchors.Info, "- fps");
            }
        }

        private void UpdateFps() {
            if(null != displayTooltip) {
                Tooltips.SetTooltipVisibility(displayTooltip, showFps);
            }

            if(!showFps) {
                return;
            }

            // Initialize
            if(null == fpsBuffer || fpsBuffer.Length != fpsFrameRange) {
                if(fpsFrameRange <= 0) { fpsFrameRange = 1; }
                fpsBuffer = new int[fpsFrameRange];
                fpsBufferIndex = 0;
            }

            // Bufferize
            fpsBuffer[fpsBufferIndex] = (int) (1f / Time.unscaledDeltaTime);
            ++fpsBufferIndex;
            if(fpsBufferIndex >= fpsFrameRange) {
                fpsBufferIndex = 0;
            }

            // Calculate mean fps
            int sum = 0;
            for(int i = 0; i < fpsFrameRange; ++i) {
                sum += fpsBuffer[i];
            }
            fps = sum / fpsFrameRange;

            if(null != displayTooltip) {
                Tooltips.SetTooltipText(displayTooltip, $"{fps} fps");
            }
        }

        private void Update()
        {
            UpdateFps();
        }

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }

        public static void SetDisplayGizmos(bool value)
        {
            displayGizmos = value;
            ShowHideControllersGizmos(FindObjectsOfType<LightController>() as LightController[], value);
            ShowHideControllersGizmos(FindObjectsOfType<CameraController>() as CameraController[], value);
        }

        public static void ShowHideControllersGizmos(ParametersController[] controllers, bool value)
        {
            foreach(var controller in controllers)
            {
                MeshFilter[] meshFilters = controller.gameObject.GetComponentsInChildren<MeshFilter>(true);
                foreach(MeshFilter meshFilter in meshFilters)
                {
                    meshFilter.gameObject.SetActive(value);
                }
            }
        }

        public static bool CanUseControls(NavigationMode.UsedControls controls)
        {
            return (currentNavigationMode == null) ? true : !currentNavigationMode.usedControls.HasFlag(controls);
        }

        public void OnFlightSpeedChange(float value)
        {
            flightSpeed = value;
        }
        public void OnFlightRotationSpeedChange(float value)
        {
            flightRotationSpeed = value;
        }
        public void OnFlightDampingChange(float value)
        {
            flightDamping = value;
        }
    }
}
