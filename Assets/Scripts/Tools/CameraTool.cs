using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class CameraTool : SelectorBase
    {
        // Start is called before the first frame update
        public GameObject cameraPrefab;
        public Transform cameraContainer;
        public Material screenShotMaterial;
        public Transform world;
        public Transform backgroundFeedback;
        public Transform dopesheetHandle = null;
        public Transform cameraPreviewHandle = null;
        public TextMeshProUGUI tm;
        public float filmHeight = 24f;  // mm
        public float zoomSpeed = 1f;
        public RenderTexture renderTexture = null;

        private float focal;
        private float cameraFeedbackScale = 1f;
        private float cameraFeedbackScaleFactor = 1.1f;
        private GameObject UIObject = null;
        private bool feedbackPositioning = false;
        private Transform focalSlider = null;

        private bool showTimeline = false;
        private Transform showDopesheetCheckbox = null;
        private Dopesheet dopesheet;

        private bool showCameraPreview = false;
        private Transform showCameraPreviewCheckbox = null;
        private CameraPreviewWindow cameraPreviewWindow;

        public float deadZone = 0.8f;

        private Transform controller;
        private Vector3 cameraPreviewDirection = new Vector3(0, 1, 1);

        public float Focal
        {
            get { return focal; }
            set 
            { 
                focal = value;
                foreach (KeyValuePair<int, GameObject> data in Selection.selection)
                {
                    GameObject gobject = data.Value;
                    CameraController cameraControler = gobject.GetComponent<CameraController>();
                    if (null == cameraControler)
                        continue;
                    cameraControler.focal = value;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            OnSelectionChanged(null, null);
            foreach (Camera camera in SelectedCameras())
                ComputeFocal(camera);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            feedbackPositioning = false;
        }

        void DisableUI()
        {
            focalSlider.gameObject.SetActive(false);
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
                showDopesheetCheckbox = panel.Find("ShowDopesheet");
                showCameraPreviewCheckbox = panel.Find("ShowCameraPreview");
            }

            if (!dopesheetHandle)
            {
                Debug.LogWarning("You forgot to give the Dopesheet to the Camera Tool.");
            }
            else
            {
                dopesheet = dopesheetHandle.GetComponentInChildren<Dopesheet>();
                dopesheetHandle.transform.localScale = Vector3.zero;
            }

            if (!cameraPreviewHandle)
            {
                Debug.LogWarning("You forgot to give the CameraPreview to the Camera Tool.");
            }
            else
            {
                cameraPreviewWindow = cameraPreviewHandle.GetComponentInChildren<CameraPreviewWindow>();
                cameraPreviewHandle.transform.localScale = Vector3.zero;
            }

            DisableUI();

            Init();
            ToolsUIManager.Instance.OnToolParameterChangedEvent += OnChangeParameter;
            //ToolsUIManager.Instance.OnBoolToolParameterChangedEvent += OnBoolChangeParameter;

            cameraPreviewDirection = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

            Selection.OnSelectionChanged += OnSelectionChanged;

            // Create tooltips
            CreateTooltips();
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Joystick, "Zoom");
        }

        protected void UpdateCameraFeedback(Vector3 position, Vector3 direction)
        {
            List<Camera> cameras = SelectedCameras();
            if (cameras.Count > 0)
            {

                float far = Camera.main.farClipPlane * 0.7f;
                backgroundFeedback.position = position + direction.normalized * far;
                backgroundFeedback.rotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, 180, 0);
                float scale = far * Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f) * 0.5f * cameraFeedbackScale;

                Camera cam = cameras[0].GetComponentInChildren<Camera>();
                backgroundFeedback.gameObject.SetActive(true);
                backgroundFeedback.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", cam.targetTexture);
                backgroundFeedback.localScale = new Vector3(scale * cam.aspect, scale, scale);
            }
            else
            {
                backgroundFeedback.gameObject.SetActive(false);
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

        public void OnCheckShowDopesheet(bool value)
        {
            showTimeline = value;
            if (dopesheet != null && dopesheetHandle != null)
            {
                if (value)
                {
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
            if (cb != null)
            {
                cb.Checked = false;
            }
        }

        public void OnCheckShowCameraPreview(bool value)
        {
            showCameraPreview = value;
            if (cameraPreviewWindow != null && cameraPreviewHandle != null)
            {
                if (value)
                {
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
            var selection = Selection.selection.Values;
            List<Camera> selectedCameras = new List<Camera>();
            foreach (var selectedItem in selection)
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
            var selection = Selection.selection.Values;
            List<GameObject> selectedCameras = new List<GameObject>();
            foreach (var selectedItem in selection)
            {
                Camera cam = selectedItem.GetComponentInChildren<Camera>();               
                if (!cam)
                    continue;
                selectedCameras.Add(selectedItem);
            }
            return selectedCameras;
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    Matrix4x4 matrix = cameraContainer.worldToLocalMatrix * selectorBrush.localToWorldMatrix * Matrix4x4.Scale(new Vector3(5f, 5f, 5f));
                    GameObject newCamera = SyncData.InstantiateUnityPrefab(cameraPrefab, matrix);
                    if (newCamera)
                    {
                        CommandGroup undoGroup = new CommandGroup();
                        new CommandAddGameObject(newCamera).Submit();
                        ClearSelection();
                        AddToSelection(newCamera);
                        undoGroup.Submit();
                    }
                }
                OnStartGrip();
            },
           () =>
           {
               OnEndGrip();
           });
        }

        public void SendCameraParams(GameObject camera)
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = camera.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
        }


        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            // Update feedback position and scale
            bool trigger = false;
            if (feedbackPositioning
                && VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton ))
            {
                cameraPreviewDirection = transform.forward;
                trigger = true;
            }
            UpdateCameraFeedback(transform.parent.parent.position, cameraPreviewDirection);
            if(trigger)
            {
                // Cam feedback scale
                Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                if (joystickAxis.y > deadZone)
                    cameraFeedbackScale *= cameraFeedbackScaleFactor;
                if (joystickAxis.y < -deadZone)
                    cameraFeedbackScale /= cameraFeedbackScaleFactor;
            }

            if (!feedbackPositioning)
            {
                base.DoUpdate(position, rotation);
            }
        }

        protected override void ShowTool(bool show)
        {
            ShowMouthpiece(selectorBrush, show);

            if (rightController != null)
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
            if (args.toolName != "Camera")
                return;
            Focal = args.value;
            foreach(Camera cam in SelectedCameras())
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
                
                // Update the Dopesheet
                //if (dopesheet != null)
                //    dopesheet.UpdateFromController(cameraController); // anim parameters? to be generic

                if (cameraPreviewWindow != null)
                    cameraPreviewWindow.UpdateFromController(cameraController);

                // Update the Camera Panel
                UISlider sliderComp = focalSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraController.focal;
                    focalSlider.gameObject.SetActive(true);
                }

                // Use only the first camera.
                return;
            }

            // if no selection

            if (dopesheet != null)
                dopesheet.Clear();

            if (cameraPreviewWindow != null)
                cameraPreviewWindow.Clear();

            focalSlider.gameObject.SetActive(false);
        }

        void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            ClearListeners();

            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if (null == cameraController)
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
            if (dopesheet != null)
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

            if (dopesheet != null)
            {
                int f = the_previous_keyframe--;
                dopesheet.CurrentFrame = f;
            }
        }
    }
}
