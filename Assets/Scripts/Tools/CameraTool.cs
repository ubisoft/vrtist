using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class MontageModeInfo
    {
        public bool montage;
    }

    public class CameraTool : SelectorBase
    {
        // Start is called before the first frame update
        public GameObject cameraPrefab;
        public Transform cameraContainer;
        public Material screenShotMaterial;
        public Transform backgroundFeedback;
        public Transform dopesheetHandle = null;
        public Transform cameraPreviewHandle = null;
        public Transform paletteHandle = null;
        public TextMeshProUGUI tm;
        public float filmHeight = 24f;  // mm
        public float zoomSpeed = 1f;
        public RenderTexture renderTexture = null;

        private float focal;
        private GameObject UIObject = null;
        private Transform focalSlider = null;

        private bool showCameraFeedback = false;
        private UICheckbox showCameraFeedbackCheckbox = null;
        private UICheckbox feedbackPositionningCheckbox = null;
        private bool feedbackPositioning = false;
        private float cameraFeedbackScale = 1f;
        private float cameraFeedbackScaleFactor = 1.1f;

        private bool showDopesheet = false;
        private bool firstTimeShowDopesheet = true;
        private UICheckbox showDopesheetCheckbox = null;

        private bool showCameraPreview = false;
        private bool firstTimeShowCameraPreview = true;
        private UICheckbox showCameraPreviewCheckbox = null;
        private CameraPreviewWindow cameraPreviewWindow;

        public static bool showCameraFrustum = false;
        private UICheckbox showCameraFrustumCheckbox = null;

        public float deadZone = 0.8f;

        private Transform controller;
        private Vector3 cameraPreviewDirection = new Vector3(0, 1, 1);

        public UIDynamicList cameraList;
        private GameObject cameraItemPrefab;

        public bool montage = false;

        public float Focal
        {
            get { return focal; }
            set
            {
                focal = value;

                foreach(GameObject gobject in SelectedCameraObjects())
                {
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if(null == cameraControler)
                        continue;
                    cameraControler.focal = value;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitUIPanel();

            OnSelectionChanged(null, null);
            foreach(Camera camera in SelectedCameras())
                ComputeFocal(camera);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            feedbackPositioning = false;
        }

        protected override void Awake()
        {
            base.Awake();

            if(!panel)
            {
                Debug.LogWarning("You forgot to give the Camera Panel to the Camera Tool.");
            }
            else
            {
                focalSlider = panel.Find("Focal");
                showCameraFeedbackCheckbox = panel.Find("ShowFeedback")?.gameObject.GetComponent<UICheckbox>();
                feedbackPositionningCheckbox = panel.Find("Feedback")?.gameObject.GetComponent<UICheckbox>();
                showDopesheetCheckbox = panel.Find("ShowDopesheet")?.gameObject.GetComponent<UICheckbox>();
                showCameraPreviewCheckbox = panel.Find("ShowCameraPreview")?.gameObject.GetComponent<UICheckbox>();
                showCameraFrustumCheckbox = panel.Find("ShowFrustum")?.gameObject.GetComponent<UICheckbox>();
            }

            if(!dopesheetHandle)
            {
                Debug.LogWarning("You forgot to give the Dopesheet to the Camera Tool.");
            }
            else
            {
                //dopesheet = dopesheetHandle.GetComponentInChildren<Dopesheet>();
                dopesheetHandle.transform.localScale = Vector3.zero; // si tous les tools ont une ref sur la dopesheet, qui la cache au demarrage? ToolsUIManager?
            }

            if(!cameraPreviewHandle)
            {
                Debug.LogWarning("You forgot to give the CameraPreview to the Camera Tool.");
            }
            else
            {
                cameraPreviewWindow = cameraPreviewHandle.GetComponentInChildren<CameraPreviewWindow>();
                cameraPreviewHandle.transform.localScale = Vector3.zero;
            }

            cameraPreviewDirection = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

            Init();

            ToolsUIManager.Instance.OnToolParameterChangedEvent += OnChangeParameter;

            // Create tooltips
            CreateTooltips();
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Joystick, "Zoom");

            // Camera list
            GlobalState.ObjectAddedEvent.AddListener(OnCameraAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnCameraRemoved);
            if(null != cameraList) { cameraList.ItemClickedEvent += OnSelectCameraItem; }
            cameraItemPrefab = Resources.Load<GameObject>("Prefabs/UI/CameraItem");
        }

        protected override void Init()
        {
            base.Init();

            //showCameraFeedback = true;
            //feedbackPositioning = true;
            showCameraFeedback = false;
            focalSlider.gameObject.SetActive(false);

            InitUIPanel();
        }

        protected void InitUIPanel()
        {
            if(showCameraFeedbackCheckbox != null)
            {
                showCameraFeedbackCheckbox.Checked = showCameraFeedback;
            }

            if(feedbackPositionningCheckbox != null)
            {
                feedbackPositionningCheckbox.Checked = feedbackPositioning;
                feedbackPositionningCheckbox.Disabled = !showCameraFeedback;
            }

            if(null != showCameraFrustumCheckbox)
            {
                showCameraFrustumCheckbox.Checked = showCameraFrustum;
            }
        }

        public void OnCameraAdded(GameObject gObject)
        {
            CameraController cameraController = gObject.GetComponent<CameraController>();
            if(null == cameraController)
                return;
            GameObject cameraItemObject = Instantiate(cameraItemPrefab);
            CameraItem cameraItem = cameraItemObject.GetComponentInChildren<CameraItem>();
            cameraItem.SetCameraObject(gObject);
            cameraList.AddItem(cameraItem.transform);
        }

        public void OnCameraRemoved(GameObject gObject)
        {
            CameraController cameraController = gObject.GetComponent<CameraController>();
            if(null == cameraController)
                return;
            foreach(var item in cameraList.GetItems())
            {
                CameraItem cameraItem = item.Content.GetComponent<CameraItem>();
                if(cameraItem.cameraObject == gObject)
                {
                    cameraList.RemoveItem(item);
                    return;
                }
            }
        }

        protected void UpdateCameraFeedback(Vector3 position, Vector3 direction)
        {
            GameObject currentCamera = Selection.activeCamera;
            if(null != currentCamera)
            {
                float far = Camera.main.farClipPlane * 0.7f;
                backgroundFeedback.position = position + direction.normalized * far;
                backgroundFeedback.rotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, 180, 0);
                float scale = far * Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f) * 0.5f * cameraFeedbackScale;

                Camera cam = currentCamera.GetComponentInChildren<Camera>();
                backgroundFeedback.localScale = new Vector3(scale * cam.aspect, scale, scale);
            }
        }

        public override void OnUIObjectEnter(int gohash)
        {
            feedbackPositioning = false;
            UIObject = ToolsUIManager.Instance.GetUI3DObject(gohash);
        }

        public override void OnUIObjectExit(int gohash)
        {
            UIObject = null;
        }

        public void OnCheckShowCameraFeedback(bool value)
        {
            showCameraFeedback = value;

            backgroundFeedback.gameObject.SetActive(value);

            UICheckbox feedbackPositionningCB = feedbackPositionningCheckbox.GetComponent<UICheckbox>();
            if(feedbackPositionningCB != null)
            {
                feedbackPositionningCB.Disabled = !value;
            }
        }

        public void OnCheckFeedbackPositionning(bool value)
        {
            feedbackPositioning = value;
        }

        public void OnCloseDopesheet()
        {
            OnCheckShowDopesheet(false);

            UICheckbox showDopesheet = showDopesheetCheckbox.GetComponent<UICheckbox>();
            if(showDopesheet != null)
            {
                showDopesheet.Checked = false;
            }
        }

        public void OnCheckShowDopesheet(bool value)
        {
            showDopesheet = value;
            if(dopesheet != null && dopesheetHandle != null)
            {
                if(value)
                {
                    if(firstTimeShowDopesheet)
                    {
                        Vector3 offset = new Vector3(0.25f, 0.0f, 0.0f);
                        dopesheetHandle.transform.position = paletteHandle.TransformPoint(offset);
                        dopesheetHandle.transform.rotation = paletteHandle.rotation;
                        firstTimeShowDopesheet = false;
                    }
                    ToolsUIManager.Instance.OpenWindow(dopesheetHandle.transform, 1.0f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(dopesheetHandle.transform, 1.0f);
                }
            }
        }

        public void OnCloseCameraPreview()
        {
            OnCheckShowCameraPreview(false);

            UICheckbox cb = showCameraPreviewCheckbox.GetComponent<UICheckbox>();
            if(cb != null)
            {
                cb.Checked = false;
            }
        }

        public void OnCheckShowCameraPreview(bool value)
        {
            showCameraPreview = value;
            if(cameraPreviewWindow != null && cameraPreviewHandle != null)
            {
                if(value)
                {
                    if(firstTimeShowCameraPreview)
                    {
                        Vector3 offset = new Vector3(0.5f, 0.5f, 0.0f);
                        cameraPreviewHandle.transform.position = paletteHandle.TransformPoint(offset);
                        cameraPreviewHandle.transform.rotation = paletteHandle.rotation;
                        firstTimeShowCameraPreview = false;
                    }
                    ToolsUIManager.Instance.OpenWindow(cameraPreviewHandle.transform, 1.0f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(cameraPreviewHandle.transform, 1.0f);
                }
            }
        }

        private List<Camera> SelectedCameras()
        {
            List<Camera> selectedCameras = new List<Camera>();
            foreach(var selectedItem in Selection.GetObjects())
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();
                if(!cam)
                    continue;
                selectedCameras.Add(cam);
            }
            return selectedCameras;
        }

        private List<GameObject> SelectedCameraObjects()
        {
            List<GameObject> selectedCameras = new List<GameObject>();

            foreach(var selectedItem in Selection.GetObjects())
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();
                if(!cam)
                    continue;
                if(selectedItem != Selection.activeCamera)
                    selectedCameras.Add(selectedItem);
            }
            if(null != Selection.activeCamera)
                selectedCameras.Add(Selection.activeCamera);
            return selectedCameras;
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if(UIObject)
                {
                    Matrix4x4 matrix = cameraContainer.worldToLocalMatrix * selectorBrush.localToWorldMatrix * Matrix4x4.Scale(new Vector3(5f, 5f, 5f));
                    GameObject newCamera = SyncData.InstantiateUnityPrefab(cameraPrefab, matrix);
                    if(newCamera)
                    {
                        CommandGroup undoGroup = new CommandGroup();
                        new CommandAddGameObject(newCamera).Submit();
                        ClearSelection();
                        AddToSelection(newCamera);
                        undoGroup.Submit();
                        Selection.SetHoveredObject(newCamera);
                    }
                }
                OnStartGrip();
            },
            () =>
            {
                OnEndGrip();
            });

            // called to update focal slider value
            UpdateUI();
        }

        public void SendCameraParams(GameObject camera)
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = camera.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
        }

        protected override void DoUpdate()
        {
            // Update feedback position and scale
            if(showCameraFeedback)
            {
                bool trigger = false;
                if(feedbackPositioning
                    && VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton))
                {
                    cameraPreviewDirection = transform.forward;
                    trigger = true;
                }
                UpdateCameraFeedback(transform.parent.parent.position, cameraPreviewDirection);
                if(trigger)
                {
                    // Cam feedback scale
                    Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                    if(joystickAxis.y > deadZone)
                        cameraFeedbackScale *= cameraFeedbackScaleFactor;
                    if(joystickAxis.y < -deadZone)
                        cameraFeedbackScale /= cameraFeedbackScaleFactor;
                }
            }

            // called to update focal slider value
            UpdateUI();

            if(!showCameraFeedback || !feedbackPositioning)
            {
                base.DoUpdate();
            }
        }

        protected override void ShowTool(bool show)
        {
            ActivateMouthpiece(selectorBrush, show);

            if(rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        private float ComputeFocal(Camera cam)
        {
            Focal = filmHeight / (2f * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2f));
            return Focal;
        }

        private float ComputeFOV(Camera cam)
        {
            cam.fieldOfView = 2f * Mathf.Atan(filmHeight / (2f * Focal)) * Mathf.Rad2Deg;
            return cam.fieldOfView;
        }

        public void OnChangeFocal(float value)
        {
            Focal = value;
            foreach(GameObject camObject in SelectedCameraObjects())
            {
                Camera cam = camObject.GetComponentInChildren<Camera>();
                ComputeFOV(cam);
                SendCameraParams(camObject);
            }
        }

        private void OnChangeParameter(object sender, ToolParameterChangedArgs args)
        {
            // update selection parameters from UI
            if(args.toolName != "Camera")
                return;
            Focal = args.value;
            foreach(Camera cam in SelectedCameras())
                ComputeFOV(cam);
        }

        protected override void UpdateUI()
        {
            // updates the panel from selection
            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if(null == cameraController)
                    continue;

                //if (cameraPreviewWindow != null)
                //    cameraPreviewWindow.UpdateFromController(cameraController);

                // Update the Camera Panel
                UISlider sliderComp = focalSlider.GetComponent<UISlider>();
                if(sliderComp != null)
                {
                    sliderComp.Value = cameraController.focal;
                    focalSlider.gameObject.SetActive(true);
                }

                // Use only the first camera.
                return;
            }

            // if no selection

            if(dopesheet != null)
                dopesheet.Clear();

            //if (cameraPreviewWindow != null)
            //    cameraPreviewWindow.Clear();

            focalSlider.gameObject.SetActive(false);
        }

        protected override void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            base.OnSelectionChanged(sender, args);

            ClearListeners();

            foreach(GameObject gobject in Selection.GetObjects())
            {
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if(null == cameraController)
                    continue;

                AddListener(cameraController);
            }

            UpdateUI();
        }

        public void OnFocalSliderPressed()
        {
            OnSliderPressed("Camera Focal", "/CameraController/focal");
        }

        public void OnAddKeyframe(int i)
        {
            // TODO:
            // - add a keyframe to the currently selected camera cameraController
        }

        public void OnRemoveKeyframe(int i)
        {
            // TODO:
            // - remove a keyframe to the currently selected camera cameraController
        }

        static int the_next_keyframe = 1; // TMP
        public void OnNextKeyframe(int currentKeyframe)
        {
            // TODO: 
            // - find the next keyframe, using the current one provided, and cameraController keyframes.
            // - call the dopesheet to tell it the new current keyframe
            if(dopesheet != null)
            {
                int f = the_next_keyframe++;
                dopesheet.CurrentFrame = f;
            }
        }

        static int the_previous_keyframe = 100; // TMP
        public void OnPreviousKeyframe(int currentKeyframe)
        {
            // TODO: 
            // - find the previous keyframe, using the current one provided, and cameraController keyframes.
            // - call the dopesheet to tell it the new current keyframe

            if(dopesheet != null)
            {
                int f = the_previous_keyframe--;
                dopesheet.CurrentFrame = f;
            }
        }

        public void OnCheckShowCameraFrustum(bool value)
        {
            showCameraFrustum = value;
        }

        public void OnSelectCameraItem(object sender, GameObjectArgs args)
        {
            GameObject item = args.gobject;
            CameraItem cameraItem = item.GetComponent<CameraItem>();

            // Select camera in scene
            CommandGroup command = new CommandGroup();
            Selection.ClearSelection();
            new CommandRemoveFromSelection(Selection.GetObjects()).Submit();
            Selection.AddToSelection(cameraItem.cameraObject);
            new CommandAddToSelection(cameraItem.cameraObject).Submit();
            command.Submit();
        }

        public void OnSetMontage(bool montage)
        {
            this.montage = montage;
            NetworkClient.GetInstance().SendEvent<MontageModeInfo>(MessageType.MontageMode, new MontageModeInfo { montage = montage });
        }
    }
}
