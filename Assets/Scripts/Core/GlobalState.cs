using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class GlobalState : MonoBehaviour
    {
        public Settings settings;
        public NetworkSettings networkSettings;

        [Header("Parameters")]
        public GameObject leftController = null;
        public GameObject colorPanel = null;
        public GameObject cameraFeedback = null;

        public static Settings Settings { get { return Instance.settings; } }
        public static AnimationEngine Animation { get { return AnimationEngine.Instance; } }

        // Connected users
        public UnityEvent onConnected = new UnityEvent();
        public static ConnectedUser networkUser = new ConnectedUser();
        private Dictionary<string, ConnectedUser> connectedUsers = new Dictionary<string, ConnectedUser>();
        private Dictionary<string, AvatarController> connectedAvatars = new Dictionary<string, AvatarController>();
        private GameObject avatarPrefab;
        private Transform avatarsContainer;

        // Selection gripped
        public bool selectionGripped = false;

        // FPS
        public static int fps { get; private set; }
        private static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        private TextMeshProUGUI leftControllerDisplay = null;

        // World
        public Transform world = null;
        public static float worldScale = 1f;
        private static bool isGrippingWorld = false;
        public BoolChangedEvent onGripWorldEvent = new BoolChangedEvent(); // Event for Grip preemption.
        public static bool IsGrippingWorld { get { return isGrippingWorld; } set { isGrippingWorld = value; Instance.onGripWorldEvent.Invoke(value); } }

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
        public static GameObjectChangedEvent ObjectRenamedEvent = new GameObjectChangedEvent();

        // Geometry Importer
        private GeometryImporter geometryImporter;
        public static GeometryImporter GeometryImporter
        {
            get { return Instance.geometryImporter; }
        }

        // Animation
        //private AnimationController animationController = new AnimationController();

        public Vector3 cameraPreviewDirection = new Vector3(0, 1, 1);

        public static void FireObjectAdded(GameObject gObject)
        {
            ObjectAddedEvent.Invoke(gObject);
        }
        public static void FireObjectRemoved(GameObject gObject)
        {
            ObjectRemovedEvent.Invoke(gObject);
        }
        public static void FireObjectRenamed(GameObject gObject)
        {
            ObjectRenamedEvent.Invoke(gObject);
        }

        public MessageBox messageBox = null;

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

            // Sky
            Sky.ApplySkyColors(settings.sky);

            // Color
            instance.colorPicker = colorPanel.GetComponentInChildren<UIColorPicker>(true);
            instance.colorPicker.CurrentColor = currentColor;
            colorChangedEvent = colorPicker.onColorChangedEvent;
            instance.colorPicker.onColorChangedEvent.AddListener(OnChangeColor);
            colorReleasedEvent = new ColorChangedEvent();
            instance.colorPicker.onReleaseEvent.AddListener(OnReleaseColor);
            colorClickedEvent = colorPicker.onClickEvent;

            geometryImporter = GetComponent<GeometryImporter>();
        }

        private void OnDestroy()
        {
            settings.Save();
        }

        private void Start()
        {
            if (null != leftController)
            {
                leftControllerDisplay = leftController.transform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
                leftControllerDisplay.text = "";
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
            if (null != leftControllerDisplay)
            {
                string infoText = worldScale < 1f ? $"Scale\n-{1f / worldScale:F2}" : $"Scale\n{worldScale:F2}";
                if (settings.displayFPS)
                {
                    UpdateFps();
                    infoText += $"\n\nFPS\n{fps}";
                }
                leftControllerDisplay.text = infoText;
            }
        }

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }

        public static void SetDisplayGizmos(bool value)
        {
            Settings.displayGizmos = value;
            SetGizmosVisible(FindObjectsOfType<LightController>(), value);
            SetGizmosVisible(FindObjectsOfType<CameraController>(), value);
            SetDisplayAvatars(value);
        }

        public static void SetDisplayAvatars(bool value)
        {
            Settings.displayAvatars = value;
            SetGizmosVisible(FindObjectsOfType<AvatarController>(), value);
        }

        public static void SetGizmoVisible(GameObject gObject, bool value)
        {
            // Disable colliders
            Collider[] colliders = gObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = value;
            }

            // Hide geometry
            MeshFilter[] meshFilters = gObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                meshFilter.gameObject.SetActive(value);
            }

            // Hide UI
            Canvas[] canvases = gObject.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                canvas.gameObject.SetActive(value);
            }
        }

        public static void SetGizmosVisible(IGizmo[] gizmos, bool value)
        {
            foreach (var gizmo in gizmos)
            {
                gizmo.SetGizmoVisible(value);
            }
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
