using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [Serializable]
    public class ActiveCameraChangedEvent : UnityEvent<GameObject, GameObject>
    {
        // Empty
    }

    /// <summary>
    /// Manage the current active camera.
    /// </summary>
    public class CameraManager
    {
        private GameObject activeCamera = null;
        public GameObject ActiveCamera
        {
            get { return activeCamera; }
            set
            {
                GameObject previousActiveCamera = activeCamera;
                activeCamera = value;
                if (null != previousActiveCamera)
                {
                    previousActiveCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);
                    previousActiveCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(false);
                }
                if (null != activeCamera)
                {
                    activeCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);
                    activeCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(true);
                }
                onActiveCameraChanged.Invoke(previousActiveCamera, activeCamera);
            }
        }
        public ActiveCameraChangedEvent onActiveCameraChanged = new ActiveCameraChangedEvent();

        static CameraManager instance = null;
        public static CameraManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new CameraManager();
                }
                return instance;
            }
        }

        CameraManager()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            Selection.onHoveredChanged.AddListener(OnHoveredChanged);
        }

        GameObject GetFirstCamera(HashSet<GameObject> objects)
        {
            foreach (GameObject o in objects)
            {
                if (null != o.GetComponent<CameraController>())
                    return o;
            }
            return null;
        }

        void UpdateActiveCamera(GameObject hoveredObject, HashSet<GameObject> currentSelection)
        {
            // --------------------------------------------
            // Check hover
            // --------------------------------------------
            GameObject hoveredCamera = null;
            if (null != hoveredObject && null != hoveredObject.GetComponent<CameraController>())
                hoveredCamera = hoveredObject;

            // Set current active camera from hovered one
            if (null != hoveredCamera && hoveredCamera != activeCamera)
            {
                // Disable previous active camera
                if (null != activeCamera)
                {
                    activeCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);
                    activeCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(false);
                }

                // Enable current active camera
                ActiveCamera = hoveredCamera;
                activeCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);
                activeCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(true);
                return;
            }

            // --------------------------------------------
            // Check selected
            // --------------------------------------------
            GameObject selectedCamera = GetFirstCamera(currentSelection);
            if (null != selectedCamera && selectedCamera != activeCamera)
            {
                // Disable previous selected camera
                if (null != activeCamera)
                {
                    activeCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);
                    activeCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(false);
                }

                // Enable new one
                ActiveCamera = selectedCamera;
                activeCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);
                activeCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(true);
                return;
            }

            if (null == hoveredCamera && null == selectedCamera)
            {
                if (null != activeCamera)
                {
                    activeCamera.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);
                    activeCamera.GetComponent<CameraController>().UpdateCameraPreviewInFront(false);
                }
                ActiveCamera = null;
            }
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            UpdateActiveCamera(Selection.HoveredObject, currentSelection);
        }

        void OnHoveredChanged(GameObject previousHover, GameObject currentHover)
        {
            UpdateActiveCamera(currentHover, Selection.SelectedObjects);
        }
    }
}
