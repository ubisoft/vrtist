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
                if (null != o && null != o.GetComponent<CameraController>())
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
