using TMPro;
using UnityEngine;

namespace VRtist
{
    public class CameraController : ParametersController
    {
        public Camera cameraObject = null;
        public float focal = 35f;
        public float near = 0.07f;
        public float far = 1000f;
        public float filmHeight = 24f;

        private UISlider focalSlider = null;
        private bool focalActionSelected;
        private CommandSetValue<float> focalValueCommand;

        private UIButton inFrontButton = null;
        public bool inFront = false;

        private UIButton nameButton = null;

        private LineRenderer frustumRenderer = null;
        private GameObject disabledLayer = null;

        private void Awake()
        {
            Init();
        }

        protected override void Start()
        {
            base.Start();
            Init();
        }

        private void Init()
        {
            if (null == cameraObject)
            {
                cameraObject = gameObject.GetComponentInChildren<Camera>(true);
            }
            if (null == frustumRenderer)
            {
                GameObject frustum = transform.Find("Frustum").gameObject;
                frustumRenderer = frustum.GetComponent<LineRenderer>();
                frustumRenderer.enabled = false;
            }
            if (null == disabledLayer)
            {
                disabledLayer = transform.Find("Rotate/PreviewDisabledLayer").gameObject;
                disabledLayer.SetActive(false);
            }
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraRenamed);

            // Init UI
            if (null == focalSlider)
            {
                nameButton = transform.Find("Rotate/Name/Name").GetComponent<UIButton>();
                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(nameButton.gameObject, minSize: 6f, maxSize: 72f, alignment: TextAlignmentOptions.Center);
                nameButton.Text = gameObject.name;
                nameButton.onReleaseEvent.AddListener(OnNameClicked);
                nameButton.NeedsRebuild = true;

                focalSlider = gameObject.GetComponentInChildren<UISlider>();
                focalSlider.onSlideEventInt.AddListener(OnFocalSliderChange);
                focalSlider.onClickEvent.AddListener(OnFocalClicked);
                focalSlider.onReleaseEvent.AddListener(OnFocalReleased);

                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(focalSlider.gameObject, minSize: 1f, maxSize: 1.5f);
                focalSlider.NeedsRebuild = true;

                inFrontButton = transform.Find("Rotate/UI/InFront").GetComponentInChildren<UIButton>();
                inFrontButton.onCheckEvent.AddListener(OnSetInFront);
                inFrontButton.NeedsRebuild = true;
            }
        }

        private void OnNameClicked()
        {
            ToolsUIManager.Instance.OpenKeyboard(OnValidateCameraRename, nameButton.transform, gameObject.name);
        }

        private void OnValidateCameraRename(string newName)
        {
            new CommandRenameGameObject("Rename Camera", gameObject, newName).Submit();
            nameButton.Text = newName;  // don't call SetName() here since we don't want to rename the gameObject before the command is really sent
        }

        private void OnSetInFront(bool value)
        {
            inFront = value;
            UpdateCameraPreviewInFront(Selection.activeCamera == gameObject);
        }

        public void UpdateCameraPreviewInFront(bool active)
        {
            if (inFront && active)
            {
                transform.Find("Rotate/CameraPreview").gameObject.layer = LayerMask.NameToLayer("InFront");
            }
            else
            {
                transform.Find("Rotate/CameraPreview").gameObject.layer = gameObject.layer;
            }
        }

        private void OnFocalSliderChange(int focal)
        {
            this.focal = focal;
            ComputeFOV();
            CameraTool.SendCameraParams(gameObject);
        }

        private float ComputeFOV()
        {
            cameraObject.fieldOfView = 2f * Mathf.Atan(filmHeight / (2f * focal)) * Mathf.Rad2Deg;
            return cameraObject.fieldOfView;
        }

        private void OnFocalClicked()
        {

            focalActionSelected = Selection.IsSelected(gameObject);
            if (!focalActionSelected)
            {
                Selection.AddToSelection(gameObject);
            }
            focalValueCommand = new CommandSetValue<float>("Camera Focal", "/CameraController/focal");
        }

        private void OnFocalReleased()
        {
            focalValueCommand.Submit();
            if (!focalActionSelected)
            {
                Selection.RemoveFromSelection(gameObject);
            }
        }

        void Update()
        {
            if (null == cameraObject)
                cameraObject = gameObject.GetComponentInChildren<Camera>(true);
            if (null != cameraObject)
            {
                float scale = GlobalState.worldScale;
                cameraObject.farClipPlane = far * scale;
                cameraObject.nearClipPlane = near * scale;
                cameraObject.focalLength = focal;

                // Active camera
                if (Selection.activeCamera == gameObject)
                {
                    if (CameraTool.showCameraFrustum && GlobalState.Settings.displayGizmos)
                        DrawFrustum();
                    else
                        frustumRenderer.enabled = false;
                    disabledLayer.SetActive(false);
                }
                // Inactive camera
                else
                {
                    frustumRenderer.enabled = false;
                    if (GlobalState.Settings.displayGizmos)
                        disabledLayer.SetActive(true);
                }
            }

            if (null != focalSlider && focalSlider.Value != focal)
            {
                focalSlider.Value = focal;
            }
        }

        public void OnCameraRenamed(GameObject gObject)
        {
            if (gObject == gameObject)
                SetName(gObject.name);
        }

        public override void CopyParameters(ParametersController otherController)
        {
            base.CopyParameters(otherController);

            CameraController other = otherController as CameraController;
            focal = other.focal;
            near = other.near;
            far = other.far;
        }

        public override void SetName(string name)
        {
            base.SetName(name);
            if (null != nameButton)
                nameButton.Text = name;
        }

        private void DrawFrustum()
        {
            frustumRenderer.enabled = true;
            frustumRenderer.gameObject.layer = LayerMask.NameToLayer("UI");  // we don't want the selection outline

            float halfWidthFactor = cameraObject.sensorSize.x * 0.5f / focal;
            float halfHeightFactor = cameraObject.sensorSize.y * 0.5f / focal;

            float nearHalfWidth = halfWidthFactor * near;
            float nearHalfHeight = halfHeightFactor * near;
            float farHalfWidth = halfWidthFactor * far;
            float farHalfHeight = halfHeightFactor * far;

            Vector3[] points = new Vector3[16];
            points[0] = new Vector3(nearHalfWidth, -nearHalfHeight, near);
            points[1] = new Vector3(nearHalfWidth, nearHalfHeight, near);
            points[2] = new Vector3(-nearHalfWidth, nearHalfHeight, near);
            points[3] = new Vector3(-nearHalfWidth, -nearHalfHeight, near);
            points[4] = new Vector3(nearHalfWidth, -nearHalfHeight, near);

            points[5] = new Vector3(farHalfWidth, -farHalfHeight, far);
            points[6] = new Vector3(farHalfWidth, farHalfHeight, far);
            points[7] = new Vector3(-farHalfWidth, farHalfHeight, far);
            points[8] = new Vector3(-farHalfWidth, -farHalfHeight, far);
            points[9] = new Vector3(farHalfWidth, -farHalfHeight, far);

            points[10] = new Vector3(farHalfWidth, farHalfHeight, far);
            points[11] = new Vector3(nearHalfWidth, nearHalfHeight, near);
            points[12] = new Vector3(-nearHalfWidth, nearHalfHeight, near);
            points[13] = new Vector3(-farHalfWidth, farHalfHeight, far);
            points[14] = new Vector3(-farHalfWidth, -farHalfHeight, far);
            points[15] = new Vector3(-nearHalfWidth, -nearHalfHeight, near);

            // Remove camera object scale
            float invScale = 1f / frustumRenderer.transform.parent.lossyScale.x;
            invScale *= GlobalState.worldScale;
            frustumRenderer.transform.localScale = new Vector3(invScale, invScale, invScale);

            frustumRenderer.positionCount = points.Length;
            frustumRenderer.SetPositions(points);
        }
    }
}
