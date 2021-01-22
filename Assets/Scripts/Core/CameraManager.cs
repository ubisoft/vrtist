
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
            GameObject hoveredCamera = null;
            if (null != hoveredObject && null != hoveredObject.GetComponent<CameraController>())
                hoveredCamera = hoveredObject;
            if (null != hoveredCamera && hoveredCamera != activeCamera)
            {
                ActiveCamera = hoveredCamera;
                return;
            }

            GameObject selectedCamera = GetFirstCamera(currentSelection);
            if (null != selectedCamera && selectedCamera != activeCamera)
            {
                ActiveCamera = selectedCamera;
                return;
            }

            if (null == hoveredCamera && null == selectedCamera)
            {
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
