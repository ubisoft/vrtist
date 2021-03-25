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

using System.Collections;

using TMPro;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    /// <summary>
    /// Controller for camera.
    /// </summary>
    public class CameraController : ParametersController
    {
        private static GameObject colimatorPrefab = null;

        public Camera cameraObject = null;
        public float focal = 35f;
        public float focus = 1.0f;
        public float Focus
        {
            get { return focus; }
            set
            {
                focus = value;
                if (null != colimator)
                {
                    Vector3 direction = gameObject.transform.forward;
                    colimator.position = gameObject.transform.position - direction * focus;
                }
            }
        }
        public float aperture = 16f; // [1..32] in Unity
        public bool enableDOF = false;
        private static UnityEngine.Rendering.HighDefinition.DepthOfField dof;
        public bool EnableDOF
        {
            get { return enableDOF; }
            set
            {
                enableDOF = value;
                UpdateDOFGizmo();
                parameterChanged.Invoke();
            }
        }
        public UnityEvent parameterChanged = new UnityEvent();

        public Transform colimator = null;
        public float near = 0.07f;
        public float far = 1000f;
        public float filmHeight = 24f;

        private UISlider focalSlider = null;
        private bool focalActionSelected;
        private CommandSetValue<float> focalValueCommand;

        private UIButton inFrontButton = null;
        public bool inFront = false;

        private UITouchScreen touchScreen;
        private UIButton focusButton;

        private UIButton videoOutputButton = null;
        private UIButton snapshotButton = null;

        private bool selectionWasEnabled = true;
        private bool isVideoOutput = false;

        private UILabel nameLabel = null;

        private LineRenderer frustumRenderer = null;
        private GameObject disabledLayer = null;

        private readonly float lineRendererWidth = 0.002f;

        private LineRenderer colimatorLineRenderer;

        private Texture2D snapshot;
        public Texture2D Snapshot
        {
            get
            {
                if (null == snapshot)
                    snapshot = new Texture2D(CameraManager.Instance.CurrentResolution.width, CameraManager.Instance.CurrentResolution.height);
                return snapshot;
            }
            set
            {
                snapshot = value;
            }
        }

        private bool hacked = false;
        private void Hack()
        {
            if (hacked)
                return;
            hacked = true;

            if (null == nameLabel)
                Init();
            // Hack : force TMPro properties when component is enabled
            UIUtils.SetTMProStyle(nameLabel.gameObject, minSize: 6f, maxSize: 72f, alignment: TextAlignmentOptions.Center);
            nameLabel.NeedsRebuild = true;

            // Hack : force TMPro properties when component is enabled
            UIUtils.SetTMProStyle(focalSlider.gameObject, minSize: 1f, maxSize: 1.5f);
            focalSlider.NeedsRebuild = true;
        }

        void Start()
        {
            Init();
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraRenamed);
            GlobalState.Animation.onAnimationStateEvent.AddListener(OnRecordStateChanged);
            CameraManager.Instance.onActiveCameraChanged.AddListener(OnActiveCameraChanged);
        }

        void StopVideoOutput()
        {
            isVideoOutput = false;
            GlobalState.Animation.onFrameEvent.RemoveListener(OnFrameChanged);
            Selection.enabled = selectionWasEnabled;
            GlobalState.Animation.timeHooksEnabled = true;
        }

        void OnDisable()
        {
            if (isVideoOutput)
            {
                StopVideoOutput();
            }
        }

        private void Init()
        {
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
                focalSlider = gameObject.GetComponentInChildren<UISlider>(true);
                focalSlider.onSlideEventInt.AddListener(OnFocalSliderChange);
                focalSlider.onClickEvent.AddListener(OnFocalClicked);
                focalSlider.onReleaseEvent.AddListener(OnFocalReleased);
                // Hack : force TMPro properties when component is enabled
                UIUtils.SetTMProStyle(focalSlider.gameObject, minSize: 1f, maxSize: 1.5f);
                focalSlider.NeedsRebuild = true;

                inFrontButton = transform.Find("Rotate/UI/InFront").GetComponentInChildren<UIButton>(true);
                inFrontButton.onCheckEvent.AddListener(OnSetInFront);
                inFrontButton.NeedsRebuild = true;

                touchScreen = transform.Find("Rotate/CameraPreview/TouchScreen").GetComponentInChildren<UITouchScreen>(true);
                touchScreen.touchEvent.AddListener(OnTouch);

                focusButton = transform.Find("Rotate/UI/FocusButton").GetComponentInChildren<UIButton>(true);
                focusButton.onCheckEvent.AddListener(OnCheckFocusButton);

                videoOutputButton = transform.Find("Rotate/UI/VideoOutputButton").GetComponentInChildren<UIButton>(true);
                videoOutputButton.onCheckEvent.AddListener(OnCheckVideoOutput);

                snapshotButton = transform.Find("Rotate/UI/SnapshotButton").GetComponentInChildren<UIButton>(true);
                snapshotButton.onReleaseEvent.AddListener(OnSnapshot);

                colimatorLineRenderer = gameObject.GetComponent<LineRenderer>();
                colimatorLineRenderer.positionCount = 2;
                colimatorLineRenderer.SetPosition(0, transform.position);
                colimatorLineRenderer.startWidth = lineRendererWidth / GlobalState.WorldScale;
                colimatorLineRenderer.endWidth = lineRendererWidth / GlobalState.WorldScale;
                colimatorLineRenderer.enabled = colimator != null;
            }
        }

        private void OnTouch(Vector2 coords)
        {
            if (null == cameraObject)
                return;
            // Raycast from camera center to screen point aimed for

            float halfWidthFactor = cameraObject.sensorSize.x * 0.5f / focal;
            float halfHeightFactor = cameraObject.sensorSize.y * 0.5f / focal;

            Vector3 nearPoint = new Vector3(halfWidthFactor * coords.x * near, -halfHeightFactor * coords.y * near, -near);
            Vector3 worldNearPoint = transform.TransformPoint(nearPoint);

            Vector3 farPoint = new Vector3(halfWidthFactor * coords.x * far, -halfHeightFactor * coords.y * far, -far);
            Vector3 worldFarPoint = transform.TransformPoint(farPoint);

            Ray ray = new Ray(worldNearPoint, worldFarPoint - worldNearPoint);
            int layerMask = LayerMask.GetMask(new string[] { "Default", "Selection", "SelectionChild", "Hover", "HoverChild" });
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                if (null != colimator)
                    colimator.transform.position = hit.point;
                else
                    Focus = Vector3.Distance(hit.point, transform.position);
            }
        }

        private void OnCheckFocusButton(bool value)
        {
            touchScreen.gameObject.SetActive(value);
            if (!EnableDOF && value)
            {
                new CommandEnableDOF(gameObject, true).Submit();
            }
        }

        private void OnCheckVideoOutput(bool value)
        {
            if (value)
            {
                Selection.Clear();
                Selection.AddToSelection(gameObject);
                selectionWasEnabled = Selection.enabled;
                Selection.enabled = false;

                GlobalState.Animation.timeHooksEnabled = false;
                GlobalState.Animation.onFrameEvent.AddListener(OnFrameChanged);

                isVideoOutput = true;

                GlobalState.Animation.CurrentFrame = GlobalState.Animation.StartFrame;
            }

            GlobalState.Animation.OnToggleStartVideoOutput(value);
        }

        void OnSnapshot()
        {
            var resolution = CameraManager.Instance.CurrentResolution;

            IEnumerator DoSnapshot()
            {
                // Wait for 2 frames (sometimes the rendering is wrong)
                yield return null;
                yield return new WaitForEndOfFrame();
                Snapshot = null;
                TakeSnapshot();
                Utils.SavePNG(Snapshot, System.IO.Path.Combine(GlobalState.Settings.snapshotsDirectory, GlobalState.Settings.ProjectName + "_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png"));
                Snapshot = null;
                CameraManager.Instance.CurrentResolution = resolution;
            }

            Selection.Clear();
            Selection.AddToSelection(gameObject);
            CameraManager.Instance.CurrentResolution = CameraManager.Instance.videoOutputResolution;
            SoundManager.Instance.PlayUISound(SoundManager.Sounds.Snapshot);
            StartCoroutine(DoSnapshot());
        }

        void OnRecordStateChanged(AnimationState animationState)
        {
            bool buttonsDisabled = GlobalState.Animation.animationState == AnimationState.VideoOutput;
            snapshotButton.Disabled = buttonsDisabled;
            inFrontButton.Disabled = buttonsDisabled;
            focusButton.Disabled = buttonsDisabled;
            focalSlider.Disabled = buttonsDisabled;

            videoOutputButton.Disabled = buttonsDisabled && !isVideoOutput;
            if (isVideoOutput && GlobalState.Animation.animationState != AnimationState.VideoOutput)
            {
                videoOutputButton.Disabled = false;
                StopVideoOutput();
            }
        }

        void OnFrameChanged(int frame)
        {
            if (GlobalState.Animation.animationState == AnimationState.VideoOutput)
            {
                if (frame == GlobalState.Animation.EndFrame)
                {
                    GlobalState.Animation.Pause();
                }
            }
        }

        public override bool IsSnappable()
        {
            return false;
        }

        public override bool IsDeformable()
        {
            return false;
        }

        public override void SetGizmoVisible(bool value)
        {
            base.SetGizmoVisible(value);
            if (null != disabledLayer)
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
            UpdateCameraPreviewInFront(CameraManager.Instance.ActiveCamera == gameObject);
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

        private void ComputeFOV()
        {
            if (null != cameraObject)
                cameraObject.fieldOfView = 2f * Mathf.Atan(filmHeight / (2f * focal)) * Mathf.Rad2Deg;
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

        public GameObject CreateColimator()
        {
            if (null == colimatorPrefab)
            {
                colimatorPrefab = Resources.Load<GameObject>("Prefabs/UI/Colimator");
            }

            GameObject colimatorObject = SceneManager.InstantiateUnityPrefab(colimatorPrefab);
            colimatorObject = SceneManager.AddObject(colimatorObject);

            SceneManager.SetObjectParent(colimatorObject, gameObject);

            colimator = colimatorObject.transform;

            ColimatorController colimatorController = colimatorObject.GetComponent<ColimatorController>();
            colimatorController.isVRtist = true;

            UpdateDOFGizmo();

            return colimatorObject;
        }

        private void TakeSnapshot()
        {
            RenderTexture.active = CameraManager.Instance.RenderTexture;
            Snapshot.ReadPixels(new Rect(0, 0, CameraManager.Instance.CurrentResolution.width, CameraManager.Instance.CurrentResolution.height), 0, 0);
            Snapshot.Apply();
            GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", Snapshot);
            RenderTexture.active = null;
        }

        public void SetVirtualCamera(Camera cam)
        {
            if (null == cam)
            {
                TakeSnapshot();
            }
            cameraObject = cam;
            UpdateCameraPreviewInFront(null != cam);
            Update();
        }

        void UpdateDOFGizmo()
        {
            if (null == colimatorLineRenderer)
                colimatorLineRenderer = gameObject.GetComponent<LineRenderer>();
            colimatorLineRenderer.enabled = EnableDOF && CameraManager.Instance.ActiveCamera == gameObject && GlobalState.Settings.DisplayGizmos;
        }

        void OnActiveCameraChanged(GameObject oldCamera, GameObject newCamera)
        {
            UpdateDOFGizmo();
        }

        void Update()
        {
            Hack();

            if (null != cameraObject)
            {
                if (gameObject.name != nameLabel.Text)
                    nameLabel.Text = gameObject.name;

                cameraObject.farClipPlane = far;
                cameraObject.nearClipPlane = near;
                cameraObject.focalLength = focal;
                ComputeFOV();
                if (null != colimator)
                {
                    if (enableDOF)
                    {
                        cameraObject.GetComponent<HDAdditionalCameraData>().physicalParameters.aperture = aperture;
                        focus = Vector3.Distance(colimator.position, transform.position);

                        colimatorLineRenderer.startWidth = lineRendererWidth / GlobalState.WorldScale;
                        colimatorLineRenderer.endWidth = lineRendererWidth / GlobalState.WorldScale;
                        colimatorLineRenderer.SetPosition(0, transform.position);
                        colimatorLineRenderer.SetPosition(1, colimator.position);
                    }
                }

                if (null == dof) Utils.FindCameraPostProcessVolume().profile.TryGet(out dof);
                dof.focusDistance.value = focus;
                if (dof.active != enableDOF)
                    dof.active = enableDOF;

                // Active camera                
                if (CameraTool.showCameraFrustum && GlobalState.Settings.DisplayGizmos)
                    DrawFrustum();
                else
                    frustumRenderer.enabled = false;
                disabledLayer.SetActive(false);
            }
            else
            {
                frustumRenderer.enabled = false;
                if (GlobalState.Settings.DisplayGizmos)
                    disabledLayer.SetActive(true);
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

            frustumRenderer.positionCount = points.Length;
            frustumRenderer.SetPositions(points);
            frustumRenderer.startWidth = lineRendererWidth / GlobalState.WorldScale;
            frustumRenderer.endWidth = lineRendererWidth / GlobalState.WorldScale;
        }
    }
}
