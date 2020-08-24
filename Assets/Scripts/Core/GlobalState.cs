using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ConnectedUser
    {
        public string id;  // clientId
        public string viewId;
        public string masterId;
        public string name;
        public string room;
        public Vector3 eye;
        public Vector3 target;
        public Color color;
        public Vector3[] corners = new Vector3[4];
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

        // Connected users
        public UnityEvent onConnected = new UnityEvent();
        public static ConnectedUser networkUser = new ConnectedUser();
        private Dictionary<string, ConnectedUser> connectedUsers = new Dictionary<string, ConnectedUser>();
        private Dictionary<string, AvatarController> connectedAvatars = new Dictionary<string, AvatarController>();
        private GameObject avatarPrefab;
        private Transform avatarsContainer;

        // Play / Pause
        public bool isPlaying = false;
        public BoolChangedEvent onPlayingEvent = new BoolChangedEvent();
        // Record
        public enum RecordState { Stopped, Preroll, Recording };
        public RecordState recordState = RecordState.Stopped;
        public BoolChangedEvent onRecordEvent = new BoolChangedEvent();
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

        // Animation
        private AnimationController animationController = new AnimationController();

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
            networkSettings.Load();

            // Get network settings
            networkUser.name = networkSettings.userName;
            if (null == networkUser.name || networkUser.name.Length == 0)
                networkUser.name = "VRtist";

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
            countdown.onCountdownFinished.AddListener(OnCountdownFinished);
        }

        // Animation helpers
        ///////////////////////////
        public void AddAnimationListener(UnityAction<GameObject> callback)
        {
            animationController.AddListener(callback);
        }
        public void RemoveAnimationListener(UnityAction<GameObject> callback)
        {
            animationController.RemoveListener(callback);
        }
        public void ClearAnimations(GameObject gameObject)
        {
            animationController.ClearAnimations(gameObject);
        }
        public void FireAnimationChanged(GameObject gameObject)
        {
            animationController.FireAnimationChanged(gameObject);
        }

        public void AddAnimationChannel(GameObject gameObject, string channelName, List<AnimationKey> keys)
        {
            animationController.AddAnimationChannel(gameObject, channelName, keys);
        }
        public bool HasAnimation(GameObject gameObject)
        {
            return animationController.HasAnimation(gameObject);
        }

        public Dictionary<string, AnimationChannel> GetAnimationChannels(GameObject gameObject)
        {
            return animationController.GetAnimationChannels(gameObject);
        }

        public void AddKeyframe()
        {
            animationController.AddKeyframe();
        }

        public void RemoveKeyframe()
        {
            animationController.RemoveKeyframe();
        }

        public void Record()
        {
            if (recordState != RecordState.Stopped)
                return;

            recordState = RecordState.Preroll;
            countdown.gameObject.SetActive(true);
        }

        public void Play()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Play, 0);
            GlobalState.Instance.SetPlaying(true);
        }

        public void Pause()
        {
            switch (recordState)
            {
                case RecordState.Preroll:
                    recordState = RecordState.Stopped;
                    countdown.gameObject.SetActive(false);
                    break;
                case RecordState.Recording:
                    recordState = RecordState.Stopped;
                    animationController.ApplyAnimations();
                    countdown.gameObject.SetActive(false);
                    onRecordEvent.Invoke(false);
                    break;

            }
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Pause, 0);
            GlobalState.Instance.SetPlaying(false);
        }

        public void SetPlaying(bool value)
        {
            isPlaying = value;
            onPlayingEvent.Invoke(value);
        }

        public void OnCountdownFinished()
        {
            recordState = RecordState.Recording;
            animationController.OnCountdownFinished();
            onRecordEvent.Invoke(true);
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
            animationController.Update();
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
        }

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }

        public static void SetDisplayGizmos(bool value)
        {
            Settings.displayGizmos = value;
            ShowHideComponentsGizmos(FindObjectsOfType<LightController>(), value);
            ShowHideComponentsGizmos(FindObjectsOfType<CameraController>(), value);
        }

        public static void SetDisplayAvatars(bool value)
        {
            Settings.displayAvatars = value;
            ShowHideComponentsGizmos(FindObjectsOfType<AvatarController>(), value);
        }

        public static void ShowHideComponentsGizmos(Component[] components, bool value)
        {
            foreach (var component in components)
            {
                // Hide geometry
                MeshFilter[] meshFilters = component.gameObject.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    meshFilter.gameObject.SetActive(value);
                }

                // Hide UI
                Canvas[] canvases = component.gameObject.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas canvas in canvases)
                {
                    canvas.gameObject.SetActive(value);
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

        // Connected users
        public static void SetClientId(string id)
        {
            networkUser.id = id;
            Instance.onConnected.Invoke();
            RemoveConnectedUser(id);
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
            AvatarController controller = avatar.GetComponent<AvatarController>();
            controller.SetUser(user);
            Instance.connectedAvatars[user.id] = controller;
        }

        public static void RemoveConnectedUser(string userId)
        {
            if (Instance.connectedUsers.ContainsKey(userId))
            {
                Instance.connectedUsers.Remove(userId);
                GameObject.Destroy(Instance.connectedAvatars[userId].gameObject);
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
