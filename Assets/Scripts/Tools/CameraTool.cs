/* MIT License
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

using TMPro;

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;

namespace VRtist
{
    public class CameraTool : SelectorBase
    {
        public Transform rig;
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
        private readonly float cameraFeedbackScaleFactor = 1.05f;

        private UICheckbox showDopesheetCheckbox = null;
        private UICheckbox showShotManagerCheckbox = null;
        private UICheckbox showCameraPreviewCheckbox = null;
        private CameraPreviewWindow cameraPreviewWindow;

        public static bool showCameraFrustum = false;
        private UICheckbox showCameraFrustumCheckbox = null;

        public float deadZone = 0.8f;
        public UIDynamicList cameraList;
        private GameObject cameraItemPrefab;

        private readonly List<CameraController> selectedCameraControllers = new List<CameraController>();

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
                foreach (GameObject gobject in SelectedCameraObjects())
                {
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if (null == cameraControler)
                        continue;
                    cameraControler.Focus = value;
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
            foreach (CameraController cameraController in selectedCameraControllers)
            {
                cameraController.parameterChanged.RemoveListener(OnCameraParameterChanged);
            }
            selectedCameraControllers.Clear();
            feedbackPositioning = false;
        }

        protected override void OnSettingsChanged()
        {
            InitUIPanel();
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
                enableDepthOfFieldCheckbox = panel.Find("EnableDepthOfField").gameObject.GetComponent<UICheckbox>();
                showCameraFeedbackCheckbox = panel.Find("ShowFeedback").gameObject.GetComponent<UICheckbox>();
                feedbackPositionningCheckbox = panel.Find("Feedback").gameObject.GetComponent<UICheckbox>();
                showDopesheetCheckbox = panel.Find("ShowDopesheet").gameObject.GetComponent<UICheckbox>();
                showShotManagerCheckbox = panel.Find("ShowShotManager").gameObject.GetComponent<UICheckbox>();
                showCameraPreviewCheckbox = panel.Find("ShowCameraPreview").gameObject.GetComponent<UICheckbox>();
                showCameraFrustumCheckbox = panel.Find("ShowFrustum").gameObject.GetComponent<UICheckbox>();
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


            Init();

            // Create tooltips
            SetTooltips();
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Joystick, Tooltips.Action.HoldVertical, "Zoom");

            // Camera list
            GlobalState.ObjectAddedEvent.AddListener(OnCameraAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnCameraRemoved);
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraRenamed);
            SceneManager.clearSceneEvent.AddListener(OnClearScene);
            if (null != cameraList) { cameraList.ItemClickedEvent += OnSelectCameraItem; }
            cameraItemPrefab = Resources.Load<GameObject>("Prefabs/UI/CameraItem");
        }

        void Start()
        {
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

            if (null != showCameraFeedbackCheckbox)
            {
                showCameraFeedbackCheckbox.Checked = GlobalState.Settings.cameraFeedbackVisible;
            }
            if (null != showDopesheetCheckbox)
            {
                showDopesheetCheckbox.Checked = GlobalState.Settings.DopeSheetVisible;
            }
            if (null != showShotManagerCheckbox)
            {
                showShotManagerCheckbox.Checked = GlobalState.Settings.ShotManagerVisible;
            }
            if (null != showCameraPreviewCheckbox)
            {
                showCameraPreviewCheckbox.Checked = GlobalState.Settings.CameraPreviewVisible;
            }

        }

        private void OnCameraRenamed(GameObject gObject)
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

        private void OnCameraAdded(GameObject gObject)
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

        private void OnCameraRemoved(GameObject gObject)
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

        private void OnClearScene()
        {
            cameraList.Clear();
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
            if (dopesheet != null && dopesheetHandle != null)
            {
                if (value)
                {
                    ToolsUIManager.Instance.OpenWindow(dopesheetHandle, 0.7f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(dopesheetHandle, 0.7f);
                }
            }
            GlobalState.Settings.DopeSheetVisible = value;
        }

        public void OnCheckShowShotManager(bool value)
        {
            GlobalState.Settings.ShotManagerVisible = value;
            if (shotManager != null && shotManagerHandle != null)
            {
                if (value)
                {
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
            foreach (var selectedItem in Selection.ActiveObjects)
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

            foreach (var selectedItem in Selection.ActiveObjects)
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();
                if (!cam)
                    continue;
                if (selectedItem != CameraManager.Instance.ActiveCamera)
                    selectedCameras.Add(selectedItem);
            }
            if (null != CameraManager.Instance.ActiveCamera)
                selectedCameras.Add(CameraManager.Instance.ActiveCamera);
            return selectedCameras;
        }


        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    Matrix4x4 matrix = cameraContainer.worldToLocalMatrix * mouthpiece.localToWorldMatrix * Matrix4x4.Scale(new Vector3(5f, 5f, 5f));
                    GameObject cameraPrefab = ResourceManager.GetPrefab(PrefabID.Camera);

                    GameObject instance = SceneManager.InstantiateUnityPrefab(cameraPrefab);
                    Vector3 position = matrix.GetColumn(3);
                    Quaternion rotation = Quaternion.AngleAxis(180, Vector3.forward) * Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                    Vector3 scale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

                    CommandGroup undoGroup = new CommandGroup("Instantiate Camera");
                    try
                    {
                        ClearSelection();
                        CommandAddGameObject command = new CommandAddGameObject(instance);
                        command.Submit();
                        GameObject newCamera = command.newObject;
                        AddToSelection(newCamera);
                        SceneManager.SetObjectTransform(instance, position, rotation, scale);
                        Selection.HoveredObject = newCamera;
                    }
                    finally
                    {
                        undoGroup.Submit();
                        UIObject = null;
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
            SceneManager.SendCameraInfo(camera.transform);
        }

        protected override void DoUpdate()
        {
            // Update feedback position and scale
            if (GlobalState.Settings.cameraFeedbackVisible)
            {
                bool trigger = false;
                if (feedbackPositioning
                    && VRInput.GetValue(VRInput.primaryController, CommonUsages.gripButton))
                {
                    GlobalState.Settings.cameraFeedbackDirection = rig.InverseTransformDirection(transform.forward); // direction local to rig
                    trigger = true;
                }
                if (trigger)
                {
                    // Cam feedback scale
                    Vector2 joystickAxis = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                    float value = GlobalState.Settings.cameraFeedbackScaleValue;
                    if (joystickAxis.y > deadZone)
                    {
                        value *= cameraFeedbackScaleFactor;
                    }
                    if (joystickAxis.y < -deadZone)
                    {
                        value /= cameraFeedbackScaleFactor;
                    }
                    GlobalState.Settings.cameraFeedbackScaleValue = Mathf.Clamp(value, GlobalState.Settings.cameraFeedbackMinScaleValue, GlobalState.Settings.cameraFeedbackMaxScaleValue);
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
                CameraController cameraController = camObject.GetComponent<CameraController>();
                Vector3 direction = (cameraController.colimator.position - camObject.transform.position).normalized;
                cameraController.colimator.position = camObject.transform.position + value * direction;

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
            foreach (GameObject gobject in Selection.SelectedObjects)
            {
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if (null == cameraController)
                    continue;

                bool DOFActive = cameraController.EnableDOF;
                focusSlider.GetComponent<UISlider>().Disabled = !DOFActive;
                apertureSlider.GetComponent<UISlider>().Disabled = !DOFActive;


                //if (cameraPreviewWindow != null)
                //    cameraPreviewWindow.UpdateFromController(cameraController);

                // Update the Camera Panel
                enableDepthOfFieldCheckbox.gameObject.SetActive(true);
                enableDepthOfFieldCheckbox.Checked = cameraController.EnableDOF;

                UISlider sliderComp = focalSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.focal;
                    focalSlider.gameObject.SetActive(true);
                }

                sliderComp = focusSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.Focus;
                    focusSlider.gameObject.SetActive(true);
                }

                sliderComp = apertureSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.aperture;
                    apertureSlider.gameObject.SetActive(true);
                }

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

        private void UpdateSelectedCameraControllers()
        {
            foreach (CameraController cameraController in selectedCameraControllers)
            {
                cameraController.parameterChanged.RemoveListener(OnCameraParameterChanged);
            }

            selectedCameraControllers.Clear();
            foreach (GameObject item in Selection.SelectedObjects)
            {
                CameraController cameraController = item.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    selectedCameraControllers.Add(cameraController);
                    cameraController.parameterChanged.AddListener(OnCameraParameterChanged);
                }
            }
        }

        protected override void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            base.OnSelectionChanged(previousSelection, currentSelection);
            UpdateSelectedCameraControllers();
            foreach (GameObject item in Selection.SelectedObjects)
            {
                CameraController cameraController = item.GetComponent<CameraController>();
                if (null != cameraController)
                    cameraController.parameterChanged.AddListener(OnCameraParameterChanged);
            }
            UpdateUI();
        }

        private void OnCameraParameterChanged()
        {
            UpdateUI();
        }

        public void OnFocalSliderPressed()
        {
            OnSliderPressed("Camera Focal", "/CameraController/focal");
        }

        public void OnFocusSliderPressed()
        {
            OnSliderPressed("Camera Focus", "/CameraController/Focus");
        }
        public void OnFocusSliderReleased()
        {
            OnReleased();
        }
        public void OnApertureSliderPressed()
        {
            OnSliderPressed("Camera Aperture", "/CameraController/aperture");
        }

        public void OnCheckEnableDepthOfField(bool value)
        {
            enableDepthOfField = value;
            CommandGroup commangGroup = new CommandGroup();
            foreach (GameObject item in Selection.SelectedObjects)
            {
                CameraController cameraController = item.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    new CommandEnableDOF(item, value).Submit();
                }
            }
            commangGroup.Submit();
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
    }
}
