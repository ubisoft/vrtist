using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ConnectedUser
    {
        public string id;
        public string name;
        public Vector3 eye;
        public Vector3 target;
        public Color color;
    }

    public class GlobalState : MonoBehaviour
    {
        public Settings settings;
        public NetworkSettings networkSettings;

        [Header("Parameters")]
        public GameObject leftController = null;
        public GameObject colorPanel = null;
        public GameObject cameraFeedback = null;

        public static Settings Settings { get { return Instance.settings; } }

        [HideInInspector]
        public static string clientId;
        [HideInInspector]
        public static string masterId;

        public static string room = "Local";

        // Connected users
        private Dictionary<string, ConnectedUser> connectedUsers = new Dictionary<string, ConnectedUser>();
        private Dictionary<string, AvatarController> connectedAvatars = new Dictionary<string, AvatarController>();
        private GameObject avatarPrefab;
        private Transform avatarsContainer;

        // Play / Pause
        public bool isPlaying = false;
        public BoolChangedEvent onPlayingEvent = new BoolChangedEvent();
        // Record
        public enum RecordState { Stopped, Preroll, Recording };
        public RecordState isRecording = RecordState.Stopped;
        public BoolChangedEvent onRecordEvent = new BoolChangedEvent();
        public UnityEvent onCountdownFinished = new UnityEvent();
        public Countdown countdown = null;

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
        public bool useRayColliders = false; // DEBUG. Remove once the UI ray collision is good.

        // Color
        private static Color currentColor = Color.blue;
        public static Color CurrentColor
        {
            get { return currentColor; }
            set
            {
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

            if(null != networkSettings.master && networkSettings.master.Length > 0)
                masterId = networkSettings.master;

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

        private void Start()
        {
            if (null != leftController)
            {
                displayTooltip = Tooltips.CreateTooltip(leftController, Tooltips.Anchors.Info, "- fps");
            }
            if (null != cameraFeedback)
            {
                cameraFeedback.transform.localPosition = settings.cameraFeedbackPosition;
                cameraFeedback.transform.localRotation = settings.cameraFeedbackRotation;
                if (settings.cameraFeedbackScale.x == 0f || settings.cameraFeedbackScale.y == 0f || settings.cameraFeedbackScale.z == 0f)
                    settings.cameraFeedbackScale = new Vector3(160f, 90f, 100f);
                cameraFeedback.transform.localScale = settings.cameraFeedbackScale;
                cameraFeedback.SetActive(settings.cameraFeedbackVisible);
            }

            avatarPrefab = Resources.Load<GameObject>("Prefabs/VR Avatar");
            avatarsContainer = world.Find("Avatars");
        }

        public void SetPlaying(bool value)
        {
            isPlaying = value;
            onPlayingEvent.Invoke(value);
        }

        public void StartRecording(bool value)
        {
            if (value)
            {
                isRecording = RecordState.Preroll;
                countdown.gameObject.SetActive(true);
            }
            else
            {
                isRecording = RecordState.Stopped;
                countdown.gameObject.SetActive(false);
                onCountdownFinished.RemoveAllListeners();
                onRecordEvent.Invoke(false);
            }
        }

        public void OnCountdownFinished()
        {
            isRecording = RecordState.Recording;
            onRecordEvent.Invoke(true);
            onCountdownFinished.Invoke();
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Play, 0);
            SetPlaying(true);
        }

        private void UpdateFps()
        {
            if (!settings.displayFPS) { return; }

            // Initialize
            if (null == fpsBuffer || fpsBuffer.Length != fpsFrameRange)
            {
                if (fpsFrameRange <= 0) { fpsFrameRange = 1; }
                fpsBuffer = new int[fpsFrameRange];
                fpsBufferIndex = 0;
            }

            // Bufferize
            fpsBuffer[fpsBufferIndex] = (int) (1f / Time.unscaledDeltaTime);
            ++fpsBufferIndex;
            if (fpsBufferIndex >= fpsFrameRange)
            {
                fpsBufferIndex = 0;
            }

            // Calculate mean fps
            int sum = 0;
            for (int i = 0; i < fpsFrameRange; ++i)
            {
                sum += fpsBuffer[i];
            }
            fps = sum / fpsFrameRange;
        }

        private void Update()
        {
            // Info on the left controller
            if (null != displayTooltip)
            {
                string infoText = worldScale < 1f ? $"Scale down: {1f / worldScale:F2}" : $"Scale up: {worldScale:F2}";
                if (settings.displayFPS)
                {
                    UpdateFps();
                    infoText += $"\n{fps} fps";
                }
                Tooltips.SetTooltipText(displayTooltip, infoText);
            }

            // Connected users
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
            foreach (var controller in controllers)
            {
                MeshFilter[] meshFilters = controller.gameObject.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter meshFilter in meshFilters)
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

        public static bool HasConnectedUser(string userId)
        {
            return Instance.connectedUsers.ContainsKey(userId);
        }

        public static void AddConnectedUser(ConnectedUser user)
        {
            Instance.connectedUsers[user.id] = user;
            GameObject avatar = Instantiate(Instance.avatarPrefab, Instance.avatarsContainer);
            avatar.name = $"{user.name} {user.id}";
            //avatar.transform.localPosition = user.eye;
            //avatar.transform.LookAt(Instance.avatarsContainer.TransformPoint(user.target));
            AvatarController controller = avatar.GetComponent<AvatarController>();
            controller.SetUser(user);
            Instance.connectedAvatars[user.id] = controller;
        }

        public static void RemoveConnectedUser(string userId)
        {
            if (Instance.connectedUsers.ContainsKey(userId))
            {
                Instance.connectedUsers.Remove(userId);
                Instance.connectedAvatars.Remove(userId);
            }
        }

        public static ConnectedUser GetConnectedUser(string userId)
        {
            return Instance.connectedUsers[userId];
        }

        public static void UpdateConnectedUser(ConnectedUser user)
        {
            if (Instance.connectedAvatars.ContainsKey(user.id))
            {
                Instance.connectedAvatars[user.id].SetUser(user);
            }
        }
    }
}
