using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    /// <summary>
    /// Global states of the app.
    /// </summary>
    public class GlobalState : MonoBehaviour
    {
        public Settings settings;
        public NetworkSettings networkSettings;

        [Header("Parameters")]
        public PlayerController playerController;
        public Transform toolsController;
        public Transform paletteController;
        public GameObject colorPanel = null;
        public GameObject cameraFeedback = null;

        public static Settings Settings { get { return Instance.settings; } }
        public static AnimationEngine Animation { get { return AnimationEngine.Instance; } }

        // Connected users
        public bool mixerConnected = false;
        public UnityEvent onConnected = new UnityEvent();
        public static ConnectedUser networkUser = new ConnectedUser();
        private readonly Dictionary<string, ConnectedUser> connectedUsers = new Dictionary<string, ConnectedUser>();
        private readonly Dictionary<string, AvatarController> connectedAvatars = new Dictionary<string, AvatarController>();
        private GameObject avatarPrefab;
        private Transform avatarsContainer;

        // Selection gripped
        public bool selectionGripped = false;

        // FPS
        public static int Fps { get; private set; }
        private static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        private TextMeshProUGUI primaryControllerDisplay = null;
        private TextMeshProUGUI secondaryControllerDisplay = null;

        // World
        public Transform world = null;
        private static float worldScale = 1f;
        public static float WorldScale
        {
            get { return worldScale; }
            set { worldScale = value; onWorldScaleEvent.Invoke(); }
        }
        private static bool isGrippingWorld = false;
        public BoolChangedEvent onGripWorldEvent = new BoolChangedEvent(); // Event for Grip preemption.
        public static UnityEvent onWorldScaleEvent = new UnityEvent();
        public static bool IsGrippingWorld { get { return isGrippingWorld; } set { isGrippingWorld = value; Instance.onGripWorldEvent.Invoke(value); } }

        // Sky
        private GradientSky volumeSky;
        public SkyChangedEvent skyChangedEvent = new SkyChangedEvent();
        public SkySettings SkySettings
        {
            get
            {
                if (null == volumeSky) Utils.FindVolume().profile.TryGet(out volumeSky);
                return new SkySettings { topColor = volumeSky.top.value, middleColor = volumeSky.middle.value, bottomColor = volumeSky.bottom.value };
            }
            set
            {
                if (null == volumeSky) Utils.FindVolume().profile.TryGet(out volumeSky);
                value.topColor.a = 1f;
                value.middleColor.a = 1f;
                value.bottomColor.a = 1f;
                volumeSky.top.value = value.topColor;
                volumeSky.middle.value = value.middleColor;
                volumeSky.bottom.value = value.bottomColor;
                GlobalState.Settings.sky = value;
                skyChangedEvent.Invoke(new SkySettings { topColor = volumeSky.top.value, middleColor = volumeSky.middle.value, bottomColor = volumeSky.bottom.value });
            }
        }

        // Cursor
        public PaletteCursor cursor = null;
        public bool useRayColliders = false; // DEBUG. Remove once the UI ray collision is good.

        // Color
        public static Color CurrentColor
        {
            get { return Instance.colorPicker.CurrentColor; }
            set
            {
                if (value != Instance.colorPicker.CurrentColor)
                {
                    Instance.colorPicker.CurrentColor = value;
                    colorChangedEvent.Invoke(value);
                }
            }
        }
        private UIColorPicker colorPicker;
        public static ColorChangedEvent colorChangedEvent = new ColorChangedEvent();    // realtime change
        public static ColorChangedEvent colorReleasedEvent = new ColorChangedEvent();   // on release change
        public static UnityEvent colorClickedEvent = new UnityEvent();                  // on click

        public static GameObjectChangedEvent ObjectAddedEvent = new GameObjectChangedEvent();
        public static GameObjectChangedEvent ObjectRemovedEvent = new GameObjectChangedEvent();
        public static GameObjectChangedEvent ObjectRenamedEvent = new GameObjectChangedEvent();
        public static GameObjectChangedEvent ObjectMovingEvent = new GameObjectChangedEvent();
        public static GameObjectChangedEvent ObjectConstraintEvent = new GameObjectChangedEvent();

        public static BlenderBankListEvent blenderBankListEvent = new BlenderBankListEvent();
        public static BlenderBankImportObjectEvent blenderBankImportObjectEvent = new BlenderBankImportObjectEvent();

        public static BoolChangedEvent castShadowsEvent = new BoolChangedEvent();

        // Geometry Importer
        private GeometryImporter geometryImporter;
        public static GeometryImporter GeometryImporter
        {
            get { return Instance.geometryImporter; }
        }

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

        public static void FireObjectMoving(GameObject gobject)
        {
            ObjectMovingEvent.Invoke(gobject);
        }

        public static void FireObjectConstraint(GameObject gobject)
        {
            ObjectConstraintEvent.Invoke(gobject);
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
            GlobalState.Instance.SkySettings = settings.sky;

            // Color
            instance.colorPicker = colorPanel.GetComponentInChildren<UIColorPicker>(true);
            CurrentColor = Color.blue;
            colorChangedEvent = colorPicker.onColorChangedEvent;
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
            _ = OutlineManager.Instance;
            _ = CameraManager.Instance;
            if (null != cameraFeedback)
            {
                cameraFeedback.SetActive(settings.cameraFeedbackVisible);
            }

            avatarPrefab = Resources.Load<GameObject>("Prefabs/VR Avatar");
            avatarsContainer = world.Find("Avatars");
        }

        private void UpdateFps()
        {
            if (!settings.DisplayFPS) { return; }

            // Initialize
            if (null == fpsBuffer || fpsBuffer.Length != fpsFrameRange)
            {
                if (fpsFrameRange <= 0) { fpsFrameRange = 1; }
                fpsBuffer = new int[fpsFrameRange];
                fpsBufferIndex = 0;
            }

            // Bufferize
            fpsBuffer[fpsBufferIndex] = (int)(1f / Time.unscaledDeltaTime);
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
            Fps = sum / fpsFrameRange;
        }

        private void Update()
        {
            // Info on the secondary controller
            if (null != secondaryControllerDisplay)
            {
                string infoText = worldScale < 1f ? $"Scale\n-{1f / worldScale:F2}" : $"Scale\n{worldScale:F2}";
                if (settings.DisplayFPS)
                {
                    UpdateFps();
                    infoText += $"\n\nFPS\n{Fps}";
                }
                secondaryControllerDisplay.text = infoText;
            }

            Tooltips.UpdateOpacity();
        }

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }

        public static void SetDisplayGizmos(bool value)
        {
            Settings.DisplayGizmos = value;
            SetGizmosVisible(FindObjectsOfType<LightController>(), value);
            SetGizmosVisible(FindObjectsOfType<CameraController>(), value);
            SetGizmosVisible(FindObjectsOfType<ConstraintLineController>(), value);
            SetDisplayAvatars(value);
        }

        public static void SetDisplayLocators(bool value)
        {
            Settings.DisplayLocators = value;
            SetGizmosVisible(FindObjectsOfType<LocatorController>(), value);
        }

        public static void SetDisplayAvatars(bool value)
        {
            Settings.DisplayAvatars = value;
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
            castShadowsEvent.Invoke(value);
        }

        public void OnReleaseColor()
        {
            colorReleasedEvent.Invoke(CurrentColor);
        }

        public void OnCameraDamping(float value)
        {
            settings.cameraDamping = value;
        }

        // Connected users
        public static void SetClientId(string id)
        {
            Instance.mixerConnected = true;
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

        public static void SetPrimaryControllerVisible(bool visible)
        {
            GetPrimaryControllerTransform().gameObject.SetActive(visible);
        }

        public static Transform GetPrimaryControllerTransform()
        {
            if (Settings.rightHanded)
            {
                return Instance.toolsController.Find("right_controller");
            }
            else
            {
                return Instance.toolsController.Find("left_controller");
            }
        }

        public static Transform GetSecondaryControllerTransform()
        {
            if (Settings.rightHanded)
            {
                return Instance.paletteController.Find("left_controller");
            }
            else
            {
                return Instance.paletteController.Find("right_controller");
            }
        }

        public static Transform GetControllerTransform(VRDevice device)
        {
            if (device == VRDevice.PrimaryController) { return GetPrimaryControllerTransform(); }
            if (device == VRDevice.SecondaryController) { return GetSecondaryControllerTransform(); }
            return null;
        }

        public static void SetPrimaryControllerDisplayText(string text)
        {
            if (null != Instance.primaryControllerDisplay)
            {
                Instance.primaryControllerDisplay.text = text;
            }
        }

        public static void SetSecondaryControllerDisplayText(string text)
        {
            if (null != Instance.secondaryControllerDisplay)
            {
                Instance.secondaryControllerDisplay.text = text;
            }
        }

        public static void SetRightHanded(bool value)
        {
            if (null != Instance.secondaryControllerDisplay && Settings.rightHanded == value)
                return;

            Settings.rightHanded = value;

            GameObject leftHandleRightController = Instance.paletteController.Find("right_controller").gameObject;
            GameObject leftHandleLeftController = Instance.paletteController.Find("left_controller").gameObject;

            GameObject rightHandleRightController = Instance.toolsController.Find("right_controller").gameObject;
            GameObject rightHandleLeftController = Instance.toolsController.Find("left_controller").gameObject;

            leftHandleLeftController.SetActive(value);
            leftHandleRightController.SetActive(!value);
            rightHandleRightController.SetActive(value);
            rightHandleLeftController.SetActive(!value);

            // Update controller's displays
            Instance.primaryControllerDisplay = GetPrimaryControllerTransform().Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            Instance.secondaryControllerDisplay = GetSecondaryControllerTransform().Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            Instance.secondaryControllerDisplay.text = "";
            Instance.primaryControllerDisplay.text = "";

            // Update tooltips
            Tooltips.HideAll(VRDevice.PrimaryController);
            Tooltips.HideAll(VRDevice.SecondaryController);
            ToolBase tool = ToolsManager.CurrentTool();
            if (null != tool)
            {
                tool.SetTooltips();
            }
            Instance.playerController.HandleCommonTooltipsVisibility();

            // Move Palette
            Transform palette = Instance.paletteController.Find("PaletteHandle");
            Vector3 currentPalettePosition = palette.localPosition;
            if (Settings.rightHanded)
                palette.localPosition = new Vector3(-0.02f, currentPalettePosition.y, currentPalettePosition.z);
            else
                palette.localPosition = new Vector3(-0.2f, currentPalettePosition.y, currentPalettePosition.z);
        }
    }
}
