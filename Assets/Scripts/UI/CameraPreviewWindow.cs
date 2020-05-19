using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class CameraPreviewWindow : MonoBehaviour
    {
        private Transform handle = null;
        private Transform mainPanel = null;
        private Transform previewImagePlane = null;
        private UIVerticalSlider focalSlider = null;
        private UIVerticalSlider focusSlider = null;
        
        private CameraController currentCameraController = null;

        void Start()
        {
            handle = transform.parent;
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                previewImagePlane = mainPanel.Find("PreviewImage/Plane");
                focalSlider = mainPanel.Find("Focal")?.GetComponent<UIVerticalSlider>();
                focusSlider = mainPanel.Find("Focus")?.GetComponent<UIVerticalSlider>();
            }

            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            // updates the panel from selection
            foreach (GameObject gobject in Selection.GetObjects())
            {
                CameraController cameraController = gobject.GetComponent<CameraController>();
                if (null == cameraController)
                    continue;

                UpdateFromController(cameraController);

                // Use only the first camera.
                return;
            }

            Clear();
        }

        public void UpdateFromController(CameraController cameraController)
        {
            currentCameraController = cameraController;

            // Get the renderTexture of the camera, and set it on the material of the previewImagePanel
            RenderTexture rt = currentCameraController.gameObject.GetComponentInChildren<Camera>(true).targetTexture;
            previewImagePlane?.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", rt);

            // Update focal
            if (focalSlider != null)
            {
                focalSlider.Disabled = false;
                focalSlider.Value = cameraController.focal;
            }

            // TODO: put focus in cameraController
            if (focusSlider != null)
            {
                focusSlider.Disabled = false;
                focusSlider.Value = 0.5f * (focusSlider.maxValue - focusSlider.minValue); // cameraController.focus;
            }

            // Get the name of the camera, and set it in the title bar
            ToolsUIManager.Instance.SetWindowTitle(handle, cameraController.gameObject.name);
        }

        private void Update()
        {
            // refresh... things..
            // UpdateFromController(currentCameraController); // faudrait ca mais qui refresh que ce qui a change.
        }

        public void Clear()
        {
            currentCameraController = null;
            ToolsUIManager.Instance.SetWindowTitle(handle, "Camera Preview");
            previewImagePlane?.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", null);
            focalSlider.Value = 0.5f * (focalSlider.maxValue - focalSlider.minValue);
            focalSlider.Disabled = true;
            focusSlider.Value = 0.5f * (focusSlider.maxValue - focusSlider.minValue);
            focusSlider.Disabled = true;
        }
    }
}
