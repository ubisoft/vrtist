using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class CameraTool : Selector
    {
        // Start is called before the first frame update
        public GameObject cameraPrefab;
        public Transform cameraContainer;
        public Material screenShotMaterial;
        public Transform world;
        public Transform backgroundFeedback;
        public TextMeshProUGUI tm;
        public float filmHeight = 24f;  // mm
        public float zoomSpeed = 1f;
        private float focal;
        private float cameraFeedbackScale = 1f;
        private float cameraFeedbackScaleFactor = 1.1f;
        private GameObject UIObject = null;
        public RenderTexture renderTexture = null;
        private bool feedbackPositioning = false;
        private Transform focalSlider = null;

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
                    CameraParameters cameraParameters = cameraControler.GetParameters() as CameraParameters;
                    if (null == cameraParameters)
                        continue;
                    cameraParameters.focal = value;
                }
            }
        }

        public float deadZone = 0.8f;

        private Transform controller;
        private Vector3 cameraPreviewDirection = new Vector3(0, 1, 1);

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

            focalSlider = panel.Find("Focal");
            DisableUI();

            Init();
            ToolsUIManager.Instance.OnToolParameterChangedEvent += OnChangeParameter;
            ToolsUIManager.Instance.OnBoolToolParameterChangedEvent += OnBoolChangeParameter;

            cameraPreviewDirection = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);

            Selection.OnSelectionChanged += OnSelectionChanged;

            // Create tooltips
            CreateTooltips();
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Joystick, "Zoom");
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

        // DEPRECATED
        private void OnBoolChangeParameter(object sender, BoolToolParameterChangedArgs args)
        {
            if (args.toolName != "Camera")
                return;
            feedbackPositioning = args.value;
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
                    Matrix4x4 matrix = cameraContainer.worldToLocalMatrix * transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(0.05f, 0.05f, 0.05f));
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
            Transform sphere = gameObject.transform.Find("Sphere");
            if (sphere != null)
            {
                sphere.gameObject.SetActive(show);
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
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if (null == cameraController)
                    continue;
                CameraParameters cameraParameters = cameraController.GetParameters() as CameraParameters;
                if (null == cameraParameters)
                    continue;

                UISlider sliderComp = focalSlider.GetComponent<UISlider>();
                if (sliderComp != null)
                {
                    sliderComp.Value = cameraParameters.focal;
                    focalSlider.gameObject.SetActive(true);
                    return;
                }
            }
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
                CameraParameters cameraParameters = cameraController.GetParameters() as CameraParameters;
                if (null == cameraParameters)
                    continue;

                AddListener(cameraController);
            }

            UpdateUI();
        }

        public void OnFocalSliderPressed()
        {
            OnSliderPressed("Camera Focal", "/CameraController/parameters.focal");
        }
    }
}