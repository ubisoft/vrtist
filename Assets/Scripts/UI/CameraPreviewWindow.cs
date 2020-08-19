using UnityEngine;

namespace VRtist
{
    public class CameraPreviewWindow : MonoBehaviour
    {
        private Transform handle = null;
        private Transform mainPanel = null;
        private Transform previewImagePlane = null;
        private UIVerticalSlider focalSlider = null;
        private UIVerticalSlider focusSlider = null;
        private UILabel titleBar = null;
        private CameraController activeCameraController = null;

        void Start()
        {
            handle = transform.parent;
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                previewImagePlane = mainPanel.Find("PreviewImage/Plane");
                focalSlider = mainPanel.Find("Focal")?.GetComponent<UIVerticalSlider>();
                focusSlider = mainPanel.Find("Focus")?.GetComponent<UIVerticalSlider>();
                titleBar = transform.parent.Find("TitleBar").GetComponent<UILabel>();
            }

            Selection.OnActiveCameraChanged += OnActiveCameraChanged;

            GlobalState.Instance.onPlayingEvent.AddListener(OnPlayingChanged);
            GlobalState.Instance.onRecordEvent.AddListener(OnRecordingChanged);
        }

        private void OnPlayingChanged(bool value)
        {
            if (GlobalState.Instance.recordState != GlobalState.RecordState.Recording)
            {
                titleBar.Pushed = value;
            }
            else
            {
                titleBar.Pushed = false;
            }
        }

        private void OnRecordingChanged(bool value)
        {
            titleBar.Pushed = false;
            titleBar.Hovered = value;
        }

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);
            }
        }

        private void OnActiveCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            CameraController cameraController = args.activeCamera.GetComponent<CameraController>();
            if (null != cameraController)
                UpdateFromController(cameraController);
            else
                Clear();
        }

        public void UpdateFromController(CameraController cameraController)
        {
            activeCameraController = cameraController;

            // Get the renderTexture of the camera, and set it on the material of the previewImagePanel
            RenderTexture rt = activeCameraController.gameObject.GetComponentInChildren<Camera>(true).targetTexture;
            previewImagePlane?.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", rt);

            // Get the name of the camera, and set it in the title bar
            ToolsUIManager.Instance.SetWindowTitle(handle, cameraController.gameObject.name);
        }

        private void Update()
        {
            if(null == activeCameraController)
            {
                focalSlider.Disabled = true;
                focusSlider.Disabled = true;
                return;
            }

            // Update focal
            if (focalSlider != null)
            {
                focalSlider.Disabled = false;
                focalSlider.Value = activeCameraController.focal;
            }

            // TODO: put focus in cameraController
            if (focusSlider != null)
            {
                focusSlider.Disabled = false;
                focusSlider.Value = 0.5f * (focusSlider.maxValue - focusSlider.minValue); // cameraController.focus;
            }
        }

        public void Clear()
        {
            activeCameraController = null;
            ToolsUIManager.Instance.SetWindowTitle(handle, "Camera Preview");
            previewImagePlane?.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", null);
            focalSlider.Value = 0.5f * (focalSlider.maxValue - focalSlider.minValue);
            focalSlider.Disabled = true;
            focusSlider.Value = 0.5f * (focusSlider.maxValue - focusSlider.minValue);
            focusSlider.Disabled = true;
        }
    }
}
