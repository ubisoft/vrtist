using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class GlobalState : MonoBehaviour
    {
        public Settings settings;

        [Header("Parameters")]
        public GameObject leftController = null;
        public GameObject colorPanel = null;
        public GameObject cameraFeedback = null;

        public static Settings Settings { get { return Instance.settings; } }

        [HideInInspector]
        public static string clientId;
        [HideInInspector]
        public static string masterId;

        // FPS
        public static int fps { get; private set; }
        private static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        private GameObject displayTooltip = null;

        // World
        public Transform world = null;
        public static float worldScale = 1f;
        private static bool isGrippingWorld = false;
        public BoolChangedEvent onGripWorldEvent = new BoolChangedEvent(); // Event for Grip preemption.
        public static bool IsGrippingWorld { get { return isGrippingWorld; } set { isGrippingWorld = value; Instance.onGripWorldEvent.Invoke(value); } }

        // Animation
        public static int startFrame = 1;
        public static int endFrame = 250;
        public static int currentFrame = 1;

        // Cursor
        public PaletteCursor cursor = null;

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


        public static GameObjectChangedEvent ObjectAddedEvent = new GameObjectChangedEvent();
        public static GameObjectChangedEvent ObjectRemovedEvent = new GameObjectChangedEvent();

        public static void FireObjectAdded(GameObject gObject)
        {
            ObjectAddedEvent.Invoke(gObject);
        }
        public static void FireObjectRemoved(GameObject gObject)
        {
            ObjectRemovedEvent.Invoke(gObject);
        }

        // Singleton
        private static GlobalState instance = null;
        public static GlobalState Instance
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
            settings.Load();

            // Color
            instance.colorPicker = colorPanel.GetComponentInChildren<UIColorPicker>(true);
            instance.colorPicker.CurrentColor = currentColor;
            colorChangedEvent = colorPicker.onColorChangedEvent;
            instance.colorPicker.onColorChangedEvent.AddListener(OnChangeColor);
            colorReleasedEvent = new ColorChangedEvent();
            instance.colorPicker.onReleaseEvent.AddListener(OnReleaseColor);
            colorClickedEvent = colorPicker.onClickEvent;
        }

        private void OnDestroy()
        {
            settings.Save();
        }

        private void Start() {
            if(null != leftController) 
            {
                displayTooltip = Tooltips.CreateTooltip(leftController, Tooltips.Anchors.Info, "- fps");
            }
            if(null != cameraFeedback)
            {
                cameraFeedback.transform.localPosition = settings.cameraFeedbackPosition;
                cameraFeedback.transform.localRotation = settings.cameraFeedbackRotation;
                if (settings.cameraFeedbackScale.x == 0f || settings.cameraFeedbackScale.y == 0f || settings.cameraFeedbackScale.z == 0f)
                    settings.cameraFeedbackScale = new Vector3(160f, 90f, 100f);
                cameraFeedback.transform.localScale = settings.cameraFeedbackScale;
                cameraFeedback.SetActive(settings.cameraFeedbackVisible);
            }
        }

        private void UpdateFps() {
            if(!settings.displayFPS) { return; }

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
                if(settings.displayFPS)
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
            Settings.displayGizmos = value;
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
        public static bool IsCursorLockedOnWidget()
        {
            if (Instance && Instance.cursor)
            {
                return Instance.cursor.IsLockedOnWidget();
            }

            return false;
        }

        public void OnLightsCastShadows(bool value)
        {
            settings.castShadows = value;
        }

        public void OnChangeColor(Color color)
        {
            currentColor = color;
        }

        public void OnReleaseColor()
        {
            colorReleasedEvent.Invoke(currentColor);
        }

        public void OnCameraDamping(float value)
        {
            settings.cameraDamping = value;
        }
    }
}
