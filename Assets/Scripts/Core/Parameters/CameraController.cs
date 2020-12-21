using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    public class CameraController : ParametersController
    {
        public Camera cameraObject = null;
        public float focal = 35f;
        public float focus = 0.1f;
        public float aperture = 16f; // [1..32] in Unity
        private bool enableDOF = false;
        private static UnityEngine.Rendering.HighDefinition.DepthOfField dof;
        public bool EnableDOF
        {
            get { return enableDOF; }
            set { enableDOF = value; parameterChanged.Invoke();  }
        }
        public UnityEvent parameterChanged = new UnityEvent();

        public Transform colimator = null;
        public float near = 0.07f;
        public float far = 1000f;
        public float filmHeight = 24f;

        private UISlider focalSlider = null;
        private bool focalActionSelected;
        private CommandSetValue<float> focalValueCommand;

        private UISlider focusSlider = null;
        private bool focusActionSelected;
        private CommandSetValue<float> focusValueCommand;

        private UISlider apertureSlider = null;
        private bool apertureActionSelected;
        private CommandSetValue<float> apertureValueCommand;


        private UIButton inFrontButton = null;
        public bool inFront = false;

        private UILabel nameLabel = null;

        private LineRenderer frustumRenderer = null;
        private GameObject disabledLayer = null;

        private float frustumLineWidth = 0.0020f;

        private void Awake()
        {
            Init();
        }

        void Start()
        {
            Init();
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraRenamed);
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

            // Init UI
            if (null == focalSlider)
            {
                nameLabel = transform.Find("Rotate/Name/Name").GetComponent<UILabel>();
                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(nameLabel.gameObject, minSize: 6f, maxSize: 72f, alignment: TextAlignmentOptions.Center);
                nameLabel.Text = gameObject.name;
                nameLabel.onReleaseEvent.AddListener(OnNameClicked);
                nameLabel.NeedsRebuild = true;

                // FOCAL
                focalSlider = gameObject.GetComponentInChildren<UISlider>();
                focalSlider.onSlideEventInt.AddListener(OnFocalSliderChange);
                focalSlider.onClickEvent.AddListener(OnFocalClicked);
                focalSlider.onReleaseEvent.AddListener(OnFocalReleased);
                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(focalSlider.gameObject, minSize: 1f, maxSize: 1.5f);
                focalSlider.NeedsRebuild = true;

                // TODO: put sliders on the camera gizmo
                /*
                // FOCUS
                focusSlider = gameObject.GetComponentInChildren<UISlider>();
                focusSlider.onSlideEventInt.AddListener(OnFocusSliderChange);
                focusSlider.onClickEvent.AddListener(OnFocusClicked);
                focusSlider.onReleaseEvent.AddListener(OnFocusReleased);
                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(focusSlider.gameObject, minSize: 1f, maxSize: 1.5f);
                focusSlider.NeedsRebuild = true;

                // APERTURE
                apertureSlider = gameObject.GetComponentInChildren<UISlider>();
                apertureSlider.onSlideEventInt.AddListener(OnApertureSliderChange);
                apertureSlider.onClickEvent.AddListener(OnApertureClicked);
                apertureSlider.onReleaseEvent.AddListener(OnApertureReleased);
                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(apertureSlider.gameObject, minSize: 1f, maxSize: 1.5f);
                apertureSlider.NeedsRebuild = true;
                */

                inFrontButton = transform.Find("Rotate/UI/InFront").GetComponentInChildren<UIButton>();
                inFrontButton.onCheckEvent.AddListener(OnSetInFront);
                inFrontButton.NeedsRebuild = true;
            }
        }

        public override void SetGizmoVisible(bool value)
        {
            bool isDisabledLayerActive = disabledLayer.activeSelf;

            base.SetGizmoVisible(value);

            disabledLayer.SetActive(value);
        }

        private void OnNameClicked()
        {
            ToolsUIManager.Instance.OpenKeyboard(OnValidateCameraRename, nameLabel.transform, gameObject.name);
        }

        private void OnValidateCameraRename(string newName)
        {
            new CommandRenameGameObject("Rename Camera", gameObject, newName).Submit();
            nameLabel.Text = newName;
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

        //
        // FOCAL
        //

        private float ComputeFOV()
        {
            cameraObject.fieldOfView = 2f * Mathf.Atan(filmHeight / (2f * focal)) * Mathf.Rad2Deg;
            return cameraObject.fieldOfView;
        }


        private void OnFocalSliderChange(int focal)
        {
            this.focal = focal;
            ComputeFOV();
            CameraTool.SendCameraParams(gameObject);
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

        //
        // FOCUS
        //

        private void OnFocusSliderChange(int focus)
        {
            this.focus = focus;
            CameraTool.SendCameraParams(gameObject);
        }

        private void OnFocusClicked()
        {

            focusActionSelected = Selection.IsSelected(gameObject);
            if (!focusActionSelected)
            {
                Selection.AddToSelection(gameObject);
            }
            focusValueCommand = new CommandSetValue<float>("Camera Focus", "/CameraController/focus");
        }

        private void OnFocusReleased()
        {
            focusValueCommand.Submit();
            if (!focusActionSelected)
            {
                Selection.RemoveFromSelection(gameObject);
            }
        }

        //
        // APERTURE
        //

        private void OnApertureSliderChange(int aperture)
        {
            this.aperture = aperture;
            CameraTool.SendCameraParams(gameObject);
        }

        private void OnApertureClicked()
        {

            apertureActionSelected = Selection.IsSelected(gameObject);
            if (!apertureActionSelected)
            {
                Selection.AddToSelection(gameObject);
            }
            apertureValueCommand = new CommandSetValue<float>("Camera Aperture", "/CameraController/aperture");
        }

        private void OnApertureReleased()
        {
            apertureValueCommand.Submit();
            if (!apertureActionSelected)
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
                if (gameObject.name != nameLabel.Text)
                    nameLabel.Text = gameObject.name;

                cameraObject.farClipPlane = far;
                cameraObject.nearClipPlane = near;
                cameraObject.focalLength = focal;
                if (enableDOF)
                {
                    cameraObject.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture = aperture;
                    focus = Vector3.Distance(colimator.position, transform.position);

                    if (Selection.activeCamera == gameObject)
                    {
                        if (null == dof) Utils.FindCameraPostProcessVolume().profile.TryGet(out dof);
                        dof.focusDistance.value = focus;
                        dof.active = true;
                    }
                }
                else
                {
                    cameraObject.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture = 16f;
                    if (Selection.activeCamera == gameObject)
                    {
                        if (null == dof) Utils.FindCameraPostProcessVolume().profile.TryGet(out dof);
                        dof.active = false;
                    }
                }

                // NOTE: cant do that here because this Update is called for all cameras. Only the current one should do it.
                //DepthOfField dof;
                //Utils.FindCameraPostProcessVolume().profile.TryGet(out dof);
                //dof.focusDistance.value = focus * scale;
                //dof.active = true; // enableDepthOfField; // TODO: add and use the flag to cameracontroller.

                // Active camera
                if (Selection.activeCamera == gameObject)
                {
                    if (CameraTool.showCameraFrustum && GlobalState.Settings.DisplayGizmos)
                        DrawFrustum();
                    else
                        frustumRenderer.enabled = false;
                    disabledLayer.SetActive(false);
                }
                // Inactive camera
                else
                {
                    frustumRenderer.enabled = false;
                    if (GlobalState.Settings.DisplayGizmos)
                        disabledLayer.SetActive(true);
                }
            }

            if (null != focalSlider && focalSlider.Value != focal)
            {
                focalSlider.Value = focal;
            }

            if (null != focusSlider && focusSlider.Value != focus)
            {
                focusSlider.Value = focus;
            }

            if (null != apertureSlider && apertureSlider.Value != aperture)
            {
                apertureSlider.Value = aperture;
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
            focus = other.focus;
            aperture = other.aperture;
            enableDOF = other.enableDOF;
        }

        public override void SetName(string name)
        {
            base.SetName(name);
            if (null != nameLabel)
                nameLabel.Text = name;
        }

        private void DrawFrustum()
        {
            frustumRenderer.enabled = true;
            frustumRenderer.gameObject.layer = LayerMask.NameToLayer("CameraHidden");  // we don't want the selection outline

            // TODO: represent FOCUS and APERTURE
            // ...

            float halfWidthFactor = cameraObject.sensorSize.x * 0.5f / focal;
            float halfHeightFactor = cameraObject.sensorSize.y * 0.5f / focal;

            float nearHalfWidth = halfWidthFactor * near;
            float nearHalfHeight = halfHeightFactor * near;
            float farHalfWidth = halfWidthFactor * far;
            float farHalfHeight = halfHeightFactor * far;

            int pointCount = 16;
            if (enableDOF)
            {
                pointCount += 4;
            }

            Vector3[] points = new Vector3[pointCount];
            points[0] = new Vector3(nearHalfWidth, -nearHalfHeight, -near);
            points[1] = new Vector3(nearHalfWidth, nearHalfHeight, -near);
            points[2] = new Vector3(-nearHalfWidth, nearHalfHeight, -near);
            points[3] = new Vector3(-nearHalfWidth, -nearHalfHeight, -near);
            points[4] = new Vector3(nearHalfWidth, -nearHalfHeight, -near);

            points[5] = new Vector3(farHalfWidth, -farHalfHeight, -far);
            points[6] = new Vector3(farHalfWidth, farHalfHeight, -far);
            points[7] = new Vector3(-farHalfWidth, farHalfHeight, -far);
            points[8] = new Vector3(-farHalfWidth, -farHalfHeight, -far);
            points[9] = new Vector3(farHalfWidth, -farHalfHeight, -far);

            points[10] = new Vector3(farHalfWidth, farHalfHeight, -far);
            points[11] = new Vector3(nearHalfWidth, nearHalfHeight, -near);
            points[12] = new Vector3(-nearHalfWidth, nearHalfHeight, -near);
            points[13] = new Vector3(-farHalfWidth, farHalfHeight, -far);
            points[14] = new Vector3(-farHalfWidth, -farHalfHeight, -far);
            points[15] = new Vector3(-nearHalfWidth, -nearHalfHeight, -near);

            if(enableDOF)
            {
                /*
                points[16] = new Vector3(halfWidthFactor * focus, -halfHeightFactor * focus, -focus);
                points[17] = new Vector3(halfWidthFactor * focus, halfHeightFactor * focus, -focus);
                points[18] = new Vector3(-halfWidthFactor * focus, halfHeightFactor * focus, -focus);
                points[19] = new Vector3(-halfWidthFactor * focus, -halfHeightFactor * focus, -focus);
                points[20] = new Vector3(halfWidthFactor * focus, -halfHeightFactor * focus, -focus);
                */
                points[16] = new Vector3(halfWidthFactor * focus, -halfHeightFactor * focus, -focus);
                points[17] = new Vector3(-halfWidthFactor * focus, halfHeightFactor * focus, -focus);
                points[18] = new Vector3(-halfWidthFactor * focus, -halfHeightFactor * focus, -focus);
                points[19] = new Vector3(halfWidthFactor * focus, halfHeightFactor * focus, -focus);
            }

            // Remove camera object scale
            /*
            float invScale = 1f / frustumRenderer.transform.parent.lossyScale.x;
            invScale *= GlobalState.WorldScale;
            frustumRenderer.transform.localScale = new Vector3(invScale, invScale, invScale);
            */

            frustumRenderer.positionCount = points.Length;
            frustumRenderer.SetPositions(points);
            frustumRenderer.startWidth = frustumLineWidth / GlobalState.WorldScale;
            frustumRenderer.endWidth = frustumLineWidth / GlobalState.WorldScale;
        }
    }
}
