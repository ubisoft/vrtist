using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;

namespace VRtist
{
    public class CameraTool : SelectorBase
    {
        // Start is called before the first frame update
        public GameObject cameraPrefab;
        public Transform cameraContainer;
        public Material screenShotMaterial;
        public Transform backgroundFeedback;
        public Transform dopesheetHandle = null;
        public Transform shotManagerHandle = null;
        public Transform cameraPreviewHandle = null;
        public Transform paletteHandle = null;
        public TextMeshProUGUI tm;
        public float filmHeight = 24f;  // mm
        public float zoomSpeed = 1f;
        public RenderTexture renderTexture = null;

        private float focal;
        private float focus;
        private float aperture;
        private GameObject UIObject = null;
        private Transform focalSlider = null;
        private Transform focusSlider = null;
        private Transform apertureSlider = null;

        private UICheckbox enableDepthOfFieldCheckbox = null;
        private bool enableDepthOfField = false;

        private UICheckbox showCameraFeedbackCheckbox = null;
        private UICheckbox feedbackPositionningCheckbox = null;
        private bool feedbackPositioning = false;
        private float cameraFeedbackScaleFactor = 1.05f;

        private bool firstTimeShowDopesheet = true;
        private UICheckbox showDopesheetCheckbox = null;

        private bool firstTimeShowShotManager = true;
        private UICheckbox showShotManagerCheckbox = null;

        private bool firstTimeShowCameraPreview = true;
        private UICheckbox showCameraPreviewCheckbox = null;
        private CameraPreviewWindow cameraPreviewWindow;

        public static bool showCameraFrustum = false;
        private UICheckbox showCameraFrustumCheckbox = null;

        public float deadZone = 0.8f;

        private Transform controller;

        public UIDynamicList cameraList;
        private GameObject cameraItemPrefab;

        public bool montage = false;

        private UnityEngine.Rendering.HighDefinition.DepthOfField dof;

        public float Focal
        {
            get { return focal; }
            set
            {
                focal = value;

                foreach (GameObject gobject in SelectedCameraObjects())
                {
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if (null == cameraControler)
                        continue;
                    cameraControler.focal = value;
                }
            }
        }

        public float Focus
        {
            get { return focus; }
            set
            {
                focus = value;
                ApplyDepthOfFieldToVolume();
                foreach (GameObject gobject in SelectedCameraObjects())
                {
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if (null == cameraControler)
                        continue;
                    cameraControler.focus = value;
                }
            }
        }

        public float Aperture
        {
            get { return aperture; }
            set
            {
                aperture = value;

                foreach (GameObject gobject in SelectedCameraObjects())
                {
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if (null == cameraControler)
                        continue;
                    cameraControler.aperture = value;
                }
            }
        }

        public bool EnableDepthOfField
        {
            get { return enableDepthOfField; }
            set
            {
                enableDepthOfField = value;

                foreach (GameObject gobject in SelectedCameraObjects())
                {
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if (null == cameraControler)
                        continue;
                    cameraControler.enableDOF = value;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitUIPanel();

            OnSelectionChanged(null, null);
            foreach (Camera camera in SelectedCameras())
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

            if (!panel)
            {
                Debug.LogWarning("You forgot to give the Camera Panel to the Camera Tool.");
            }
            else
            {
                focalSlider = panel.Find("Focal"); 
                focusSlider = panel.Find("Focus");
                apertureSlider = panel.Find("Aperture");
                enableDepthOfFieldCheckbox = panel.Find("EnableDepthOfField")?.gameObject.GetComponent<UICheckbox>();
                showCameraFeedbackCheckbox = panel.Find("ShowFeedback")?.gameObject.GetComponent<UICheckbox>();
                feedbackPositionningCheckbox = panel.Find("Feedback")?.gameObject.GetComponent<UICheckbox>();
                showDopesheetCheckbox = panel.Find("ShowDopesheet")?.gameObject.GetComponent<UICheckbox>();
                showShotManagerCheckbox = panel.Find("ShowShotManager")?.gameObject.GetComponent<UICheckbox>();
                showCameraPreviewCheckbox = panel.Find("ShowCameraPreview")?.gameObject.GetComponent<UICheckbox>();
                showCameraFrustumCheckbox = panel.Find("ShowFrustum")?.gameObject.GetComponent<UICheckbox>();
            }

            if (!dopesheetHandle)
            {
                Debug.LogWarning("You forgot to give the Dopesheet to the Camera Tool.");
            }
            else
            {
                //dopesheet = dopesheetHandle.GetComponentInChildren<Dopesheet>();
                dopesheetHandle.localScale = Vector3.zero; // si tous les tools ont une ref sur la dopesheet, qui la cache au demarrage? ToolsUIManager?
                dopesheetHandle.position = Vector3.zero;
            }

            if (!shotManagerHandle)
            {
                Debug.LogWarning("You forgot to give the Shot Manager to the Camera Tool.");
            }
            else
            {
                shotManagerHandle.localScale = Vector3.zero; // si tous les tools ont une ref sur le shot manager, qui la cache au demarrage? ToolsUIManager?
                shotManagerHandle.position = Vector3.zero;
            }

            if (!cameraPreviewHandle)
            {
                Debug.LogWarning("You forgot to give the CameraPreview to the Camera Tool.");
            }
            else
            {
                cameraPreviewWindow = cameraPreviewHandle.GetComponentInChildren<CameraPreviewWindow>();
                cameraPreviewHandle.localScale = Vector3.zero;
                cameraPreviewHandle.position = Vector3.zero;
            }

            GlobalState.Instance.cameraPreviewDirection = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

            Init();

            ToolsUIManager.Instance.OnToolParameterChangedEvent += OnChangeParameter;

            // Create tooltips
            CreateTooltips();
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Joystick, "Zoom");

            // Camera list
            GlobalState.ObjectAddedEvent.AddListener(OnCameraAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnCameraRemoved);
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraRenamed);
            if (null != cameraList) { cameraList.ItemClickedEvent += OnSelectCameraItem; }
            cameraItemPrefab = Resources.Load<GameObject>("Prefabs/UI/CameraItem");
        }

        void Start()
        {
            GlobalState.Instance.cameraPreviewDirection = backgroundFeedback.forward;
            ToolsUIManager.Instance.onPaletteOpened.AddListener(OnPaletteOpened);
        }

        protected override void Init()
        {
            base.Init();

            focalSlider.gameObject.SetActive(false);
            focusSlider.gameObject.SetActive(false);
            apertureSlider.gameObject.SetActive(false);
            enableDepthOfFieldCheckbox.gameObject.SetActive(false);

            InitUIPanel();
        }

        void OnPaletteOpened()
        {
            cameraList.NeedsRebuild = true;
        }

        protected void InitUIPanel()
        {
            if (showCameraFeedbackCheckbox != null)
            {
                showCameraFeedbackCheckbox.Checked = GlobalState.Settings.cameraFeedbackVisible;
            }

            if (feedbackPositionningCheckbox != null)
            {
                feedbackPositionningCheckbox.Checked = feedbackPositioning;
                feedbackPositionningCheckbox.Disabled = !GlobalState.Settings.cameraFeedbackVisible;
            }

            if (null != showCameraFrustumCheckbox)
            {
                showCameraFrustumCheckbox.Checked = showCameraFrustum;
            }

            if (null != enableDepthOfFieldCheckbox)
            {
                enableDepthOfFieldCheckbox.Checked = enableDepthOfField;
                focusSlider.GetComponent<UISlider>().Disabled = !enableDepthOfField;
                apertureSlider.GetComponent<UISlider>().Disabled = !enableDepthOfField;
            }
        }

        public void OnCameraRenamed(GameObject gObject)
        {
            CameraController cameraController = gObject.GetComponent<CameraController>();
            if (null == cameraController)
                return;
            foreach (UIDynamicListItem item in cameraList.GetItems())
            {
                CameraItem cameraItem = item.Content.gameObject.GetComponent<CameraItem>();
                if (cameraItem.cameraObject == gObject)
                {
                    cameraItem.SetItemName(gObject.name);
                }
            }
        }

        public void OnCameraAdded(GameObject gObject)
        {
            CameraController cameraController = gObject.GetComponent<CameraController>();
            if (null == cameraController)
                return;
            GameObject cameraItemObject = Instantiate(cameraItemPrefab);
            CameraItem cameraItem = cameraItemObject.GetComponentInChildren<CameraItem>();
            cameraItem.SetCameraObject(gObject);
            UIDynamicListItem item = cameraList.AddItem(cameraItem.transform);
            item.UseColliderForUI = true;
        }

        public void OnCameraRemoved(GameObject gObject)
        {
            CameraController cameraController = gObject.GetComponent<CameraController>();
            if (null == cameraController)
                return;
            foreach (var item in cameraList.GetItems())
            {
                CameraItem cameraItem = item.Content.GetComponent<CameraItem>();
                if (cameraItem.cameraObject == gObject)
                {
                    cameraList.RemoveItem(item);
                    return;
                }
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
            GlobalState.Settings.cameraFeedbackVisible = value;

            backgroundFeedback.gameObject.SetActive(value);

            UICheckbox feedbackPositionningCB = feedbackPositionningCheckbox.GetComponent<UICheckbox>();
            if (feedbackPositionningCB != null)
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
            if (showDopesheet != null)
            {
                showDopesheet.Checked = false;
            }
        }
        public void OnCloseShotManager()
        {
            OnCheckShowShotManager(false);

            UICheckbox showShotManager = showShotManagerCheckbox.GetComponent<UICheckbox>();
            if (showShotManager != null)
            {
                showShotManager.Checked = false;
            }
        }

        public void OnCheckShowDopesheet(bool value)
        {
            GlobalState.Settings.dopeSheetVisible = value;
            if (dopesheet != null && dopesheetHandle != null)
            {
                if (value)
                {
                    if (firstTimeShowDopesheet && dopesheetHandle.position == Vector3.zero)
                    {
                        Vector3 offset = new Vector3(0.25f, 0.0f, 0.0f);
                        dopesheetHandle.position = paletteHandle.TransformPoint(offset);
                        dopesheetHandle.rotation = paletteHandle.rotation;
                        firstTimeShowDopesheet = false;
                    }
                    ToolsUIManager.Instance.OpenWindow(dopesheetHandle, 0.7f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(dopesheetHandle, 0.7f);
                }
            }
        }

        public void OnCheckShowShotManager(bool value)
        {
            GlobalState.Settings.shotManagerVisible = value;
            if (shotManager != null && shotManagerHandle != null)
            {
                if (value)
                {
                    if (firstTimeShowShotManager && shotManagerHandle.position == Vector3.zero)
                    {
                        Vector3 offset = new Vector3(0.25f, 0.0f, 0.0f);
                        shotManagerHandle.position = paletteHandle.TransformPoint(offset);
                        shotManagerHandle.rotation = paletteHandle.rotation;
                        firstTimeShowShotManager = false;
                    }
                    ToolsUIManager.Instance.OpenWindow(shotManagerHandle, 0.7f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(shotManagerHandle, 0.7f);
                }
            }
        }
        public void OnCloseCameraPreview()
        {
            OnCheckShowCameraPreview(false);

            UICheckbox cb = showCameraPreviewCheckbox.GetComponent<UICheckbox>();
            if (cb != null)
            {
                cb.Checked = false;
            }
        }

        public void OnCheckShowCameraPreview(bool value)
        {
            GlobalState.Settings.cameraPreviewVisible = value;
            if (cameraPreviewWindow != null && cameraPreviewHandle != null)
            {
                if (value)
                {
                    if (firstTimeShowCameraPreview && cameraPreviewHandle.position == Vector3.zero)
                    {
                        Vector3 offset = new Vector3(0.5f, 0.5f, 0.0f);
                        cameraPreviewHandle.position = paletteHandle.TransformPoint(offset);
                        cameraPreviewHandle.rotation = paletteHandle.rotation;
                        firstTimeShowCameraPreview = false;
                    }
                    ToolsUIManager.Instance.OpenWindow(cameraPreviewHandle, 0.7f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(cameraPreviewHandle, 0.7f);
                }
            }
        }

        private List<Camera> SelectedCameras()
        {
            List<Camera> selectedCameras = new List<Camera>();
            foreach (var selectedItem in Selection.GetGrippedOrSelection())
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();
                if (!cam)
                    continue;
                selectedCameras.Add(cam);
            }
            return selectedCameras;
        }

        private List<GameObject> SelectedCameraObjects()
        {
            List<GameObject> selectedCameras = new List<GameObject>();

            foreach (var selectedItem in Selection.GetGrippedOrSelection())
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();
                if (!cam)
                    continue;
                if (selectedItem != Selection.activeCamera)
                    selectedCameras.Add(selectedItem);
            }
            if (null != Selection.activeCamera)
                selectedCameras.Add(Selection.activeCamera);
            return selectedCameras;
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    Matrix4x4 matrix = cameraContainer.worldToLocalMatrix * mouthpiece.localToWorldMatrix * Matrix4x4.Scale(new Vector3(5f, 5f, 5f));
                    GameObject newCamera = SyncData.InstantiateUnityPrefab(cameraPrefab, matrix);
                    if (newCamera)
                    {
                        CommandGroup undoGroup = new CommandGroup("Instantiate Camera");
                        try
                        {
                            ClearSelection();
                            new CommandAddGameObject(newCamera).Submit();
                            AddToSelection(newCamera);
                            Selection.SetHoveredObject(newCamera);
                        }
                        finally
                        {
                            undoGroup.Submit();
                            UIObject = null;
                        }
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

        public static void SendCameraParams(GameObject camera)
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = camera.transform;
            CommandManager.SendEvent(MessageType.CameraAttributes, cameraInfo);
        }

        protected override void DoUpdate()
        {
            // Update feedback position and scale
            if (GlobalState.Settings.cameraFeedbackVisible)
            {
                bool trigger = false;
                if (feedbackPositioning
                    && VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton))
                {
                    GlobalState.Instance.cameraPreviewDirection = transform.forward;
                    trigger = true;
                }
                if (trigger)
                {
                    // Cam feedback scale
                    Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                    if (joystickAxis.y > deadZone)
                        GlobalState.Settings.cameraFeedbackScaleValue *= cameraFeedbackScaleFactor;
                    if (joystickAxis.y < -deadZone)
                        GlobalState.Settings.cameraFeedbackScaleValue /= cameraFeedbackScaleFactor;
                }
            }

            // called to update focal slider value
            UpdateUI();

            if (!GlobalState.Settings.cameraFeedbackVisible || !feedbackPositioning)
            {
                base.DoUpdate();
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

        private void SetCameraAperture(Camera cam, float aperture)
        {
            cam.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture = aperture;
        }

        public void OnChangeFocal(float value)
        {
            Focal = value;
            foreach (GameObject camObject in SelectedCameraObjects())
            {
                Camera cam = camObject.GetComponentInChildren<Camera>();
                ComputeFOV(cam);
                SendCameraParams(camObject);
            }
        }

        public void OnChangeFocus(float value)
        {
            Focus = value;
            foreach (GameObject camObject in SelectedCameraObjects())
            {
                Camera cam = camObject.GetComponentInChildren<Camera>();
                SendCameraParams(camObject);
            }
        }

        public void OnChangeAperture(float value)
        {
            Aperture = value;
            foreach (GameObject camObject in SelectedCameraObjects())
            {
                Camera cam = camObject.GetComponentInChildren<Camera>();
                SetCameraAperture(cam, value);
                SendCameraParams(camObject);
            }
        }

        // NOTE: deprecated??? if not, handle Focus and Aperture, using the args.parameterName
        private void OnChangeParameter(object sender, ToolParameterChangedArgs args)
        {
            // update selection parameters from UI
            if (args.toolName != "Camera")
                return;
            Focal = args.value;
            foreach (Camera cam in SelectedCameras())
                ComputeFOV(cam);
        }

        protected override void UpdateUI()
        {
            // updates the panel from selection
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if (null == cameraController)
                    continue;

                //if (cameraPreviewWindow != null)
                //    cameraPreviewWindow.UpdateFromController(cameraController);

                // Update the Camera Panel
                enableDepthOfFieldCheckbox.gameObject.SetActive(true);
                enableDepthOfFieldCheckbox.Checked = cameraController.enableDOF;

                UISlider sliderComp = focalSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.focal;
                    focalSlider.gameObject.SetActive(true);
                }

                sliderComp = focusSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.focus;
                    focusSlider.gameObject.SetActive(true);
                }

                sliderComp = apertureSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.aperture;
                    apertureSlider.gameObject.SetActive(true);
                }

                // update the focusDistance of the volume if the worldScale change.
                if (null == dof) Utils.FindCameraPostProcessVolume().profile.TryGet(out dof);
                dof.focusDistance.value = focus * GlobalState.WorldScale;
                dof.active = enableDepthOfField; // TODO: use the flag in the cameracontroller when we add it.

                // Use only the first camera.
                return;
            }

            //if (cameraPreviewWindow != null)
            //    cameraPreviewWindow.Clear();

            enableDepthOfFieldCheckbox.gameObject.SetActive(false);
            focalSlider.gameObject.SetActive(false);
            focusSlider.gameObject.SetActive(false);
            apertureSlider.gameObject.SetActive(false);
        }

        protected override void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            base.OnSelectionChanged(sender, args);
            UpdateUI();
        }

        public void OnFocalSliderPressed()
        {
            OnSliderPressed("Camera Focal", "/CameraController/focal");
        }

        public void OnFocusSliderPressed()
        {
            OnSliderPressed("Camera Focus", "/CameraController/focus");
        }

        public void OnApertureSliderPressed()
        {
            OnSliderPressed("Camera Aperture", "/CameraController/aperture");
        }

        public void OnCheckEnableDepthOfField(bool value)
        {
            if (null != focusSlider)
            {
                focusSlider.GetComponent<UISlider>().Disabled = !value;
            }
            if (null != apertureSlider)
            {
                apertureSlider.GetComponent<UISlider>().Disabled = !value;
            }

            EnableDepthOfField = value;
        }

        private void ApplyDepthOfFieldToVolume()
        {
            // Only called when we move the focus distance of a selected camera, so we can use the tool values, not the camera values.
            // No need to foreach all cameras and pick first one.

            if (null == dof) Utils.FindCameraPostProcessVolume().profile.TryGet(out dof);
            dof.focusDistance.value = focus * GlobalState.WorldScale;
            dof.active = enableDepthOfField;
        }

        // TODO: remove once we are sure it is no longer used.

        //public void OnAddKeyframe(int i)
        //{
        //    // TODO:
        //    // - add a keyframe to the currently selected camera cameraController
        //}

        //public void OnRemoveKeyframe(int i)
        //{
        //    // TODO:
        //    // - remove a keyframe to the currently selected camera cameraController
        //}

        //static int the_next_keyframe = 1; // TMP
        //public void OnNextKeyframe(int currentKeyframe)
        //{
        //    // TODO: 
        //    // - find the next keyframe, using the current one provided, and cameraController keyframes.
        //    // - call the dopesheet to tell it the new current keyframe
        //    if (dopesheet != null)
        //    {
        //        int f = the_next_keyframe++;
        //        GlobalState.Animation.CurrentFrame = f;
        //    }
        //}

        //static int the_previous_keyframe = 100; // TMP
        //public void OnPreviousKeyframe(int currentKeyframe)
        //{
        //    // TODO: 
        //    // - find the previous keyframe, using the current one provided, and cameraController keyframes.
        //    // - call the dopesheet to tell it the new current keyframe

        //    if (dopesheet != null)
        //    {
        //        int f = the_previous_keyframe--;
        //        GlobalState.Animation.CurrentFrame = f;
        //    }
        //}

        public void OnCheckShowCameraFrustum(bool value)
        {
            showCameraFrustum = value;
        }

        public void OnSelectCameraItem(object sender, IndexedGameObjectArgs args)
        {
            GameObject item = args.gobject;
            CameraItem cameraItem = item.GetComponent<CameraItem>();

            // Select camera in scene
            CommandGroup command = new CommandGroup("Select Camera");
            try
            {
                ClearSelection();
                AddToSelection(cameraItem.cameraObject);
            }
            finally
            {
                command.Submit();
            }
        }

        public void OnSetMontage(bool montage)
        {
            this.montage = montage;
            ShotManager.Instance.MontageEnabled = montage;
        }
    }
}
