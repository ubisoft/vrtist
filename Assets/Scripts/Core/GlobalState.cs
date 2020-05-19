using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class GlobalState : MonoBehaviour
    {
        [Header("Parameters")]
        public GameObject leftController = null;
        public GameObject colorPanel = null;

        // FPS
        public static bool showFps = false;
        public static int fps { get; private set; }
        public static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        private GameObject displayTooltip = null;

        // World
        public Transform world = null;
        public static float worldScale = 1f;
        private static bool isGrippingWorld = false;
        public BoolChangedEvent onGripWorldEvent = new BoolChangedEvent(); // Event for Grip preemption.
        public static bool IsGrippingWorld { get { return isGrippingWorld; } set { isGrippingWorld = value; Instance.onGripWorldEvent.Invoke(value); } }

        // Navigation
        public static NavigationMode currentNavigationMode = null;
        public static float flightSpeed = 5f;
        public static float flightRotationSpeed = 5f;
        public static float flightDamping = 5f;
        public static float fpsSpeed = 5f;
        public static float fpsRotationSpeed = 5f;
        public static float fpsDamping = 0f;
        public static float fpsGravity = 9.8f;

        public static float orbitScaleSpeed = 0.02f; // 0-1 slider en pct
        public static float orbitMoveSpeed = 0.05f; // 0-1 slider *100
        public static float orbitRotationalSpeed = 3.0f; // 0-10

        // Lights
        public static bool castShadows = false;

        // Animation
        public static int startFrame = 1;
        public static int endFrame = 250;
        public static int currentFrame = 1;

        // Gizmos
        public static bool displayGizmos = true;

        // Right-Handed
        public static bool rightHanded = true;

        // Color
        private static Color currentColor = Color.blue;
        public static Color CurrentColor {
            get { return currentColor; }
            set {
                Instance.colorPicker.CurrentColor = value;
                Instance.OnChangeColor(value);
                colorChangedEvent.Invoke(value);
            }
        }
        private UIColorPicker colorPicker;
        public static ColorChangedEvent colorChangedEvent;   // realtime change
        public static ColorChangedEvent colorReleasedEvent;  // on release change
        public static UnityEvent colorClickedEvent;          // on click

        // Singleton
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
            instance = Instance;

            // Color
            instance.colorPicker = colorPanel.GetComponentInChildren<UIColorPicker>(true);
            instance.colorPicker.CurrentColor = currentColor;
            colorChangedEvent = colorPicker.onColorChangedEvent;
            instance.colorPicker.onColorChangedEvent.AddListener(OnChangeColor);
            colorReleasedEvent = new ColorChangedEvent();
            instance.colorPicker.onReleaseEvent.AddListener(OnReleaseColor);
            colorClickedEvent = colorPicker.onClickEvent;
        }

        private void Start() {
            if(null != leftController) {
                displayTooltip = Tooltips.CreateTooltip(leftController, Tooltips.Anchors.Info, "- fps");
            }
        }

        private void UpdateFps() {
            if(!showFps) { return; }

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
        }

        private void Update()
        {
            if(null != displayTooltip)
            {
                string infoText = worldScale < 1f ? $"Scale down: {1f / worldScale:F2}" : $"Scale up: {worldScale:F2}";
                if(showFps)
                {
                    UpdateFps();
                    infoText += $"\n{fps} fps";
                }
                Tooltips.SetTooltipText(displayTooltip, infoText);
            }
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

        // TODO: put in PlayerController?
        //       d'autant plus qu'on a pas de mecanisme pour mettre a jour les sliders avec les valeurs de GlobalState
        //       (mais on peut aussi le faire dans les NavModes, avec des Find sur les sliders).
        //       et si on le fait dans PlayerController, on devra passer les nouveaux params au mode de navigation?
        //       ou alors il pourra aller les lire comme un grand.

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

        public void OnFPSSpeedChange(float value)
        {
            fpsSpeed = value;
        }
        public void OnFPSRotationSpeedChange(float value)
        {
            fpsRotationSpeed = value;
        }
        public void OnFPSDampingChange(float value)
        {
            fpsDamping = value;
        }

        public void OnOrbitScaleSpeedChange(float value)
        {
            orbitScaleSpeed = value / 100.0f;
        }

        public void OnOrbitMoveSpeedChange(float value)
        {
            orbitMoveSpeed = value / 100.0f;
        }

        public void OnOrbitRotationalSpeedChange(float value)
        {
            orbitRotationalSpeed = value;
        }
        
        public void OnLightsCastShadows(bool value)
        {
            castShadows = value;
        }

        public void OnChangeColor(Color color)
        {
            currentColor = color;
        }

        public void OnReleaseColor()
        {
            colorReleasedEvent.Invoke(currentColor);
        }
    }
}
