using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class SelectionChangedArgs : EventArgs
    {
        public Dictionary<int, GameObject> selectionBefore = new Dictionary<int, GameObject>();
    }

    public class ActiveCameraChangedArgs : EventArgs
    {
        public GameObject activeCamera = null;
    }

    public class Selection
    {
        public static Color SelectedColor = new Color(0f / 255f, 167f / 255f, 255f / 255f);
        public static Color UnselectedColor = Color.white;

        public static Dictionary<int, GameObject> selection = new Dictionary<int, GameObject>();
        public static event EventHandler<SelectionChangedArgs> OnSelectionChanged;
        public static event EventHandler<GameObjectArgs> OnGrippedObjectChanged;

        public static Material selectionMaterial;

        private static GameObject grippedObject = null;
        private static GameObject hoveredObject = null;
        private static GameObject outlinedObject = null;

        public static GameObject activeCamera = null;
        public static event EventHandler<ActiveCameraChangedArgs> OnActiveCameraChanged;

        public static void TriggerSelectionChanged()
        {
            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);
            OnSelectionChanged?.Invoke(null, args);
        }

        public static void TriggerGrippedObjectChanged()
        {
            GameObjectArgs args = new GameObjectArgs { gobject = grippedObject };
            OnGrippedObjectChanged?.Invoke(null, args);
        }

        public static void TriggerCurrentCameraChanged()
        {
            ActiveCameraChangedArgs args = new ActiveCameraChangedArgs();
            args.activeCamera = activeCamera;
            OnActiveCameraChanged?.Invoke(null, args);
        }

        public static void fillSelection(ref Dictionary<int, GameObject> s)
        {
            foreach (KeyValuePair<int, GameObject> data in selection)
                s[data.Key] = data.Value;
        }

        public static bool IsSelected(GameObject gObject)
        {
            return selection.ContainsKey(gObject.GetInstanceID());
        }

        public static void SetActiveCamera(CameraController controller)
        {
            // Set no active camera
            if (null == controller)
            {
                if (null != activeCamera)
                {
                    activeCamera.GetComponent<CameraController>().cameraObject.gameObject.SetActive(false);
                    activeCamera = null;
                    TriggerCurrentCameraChanged();
                }
                return;
            }

            // Set active camera
            GameObject obj = controller.gameObject;
            if (activeCamera == obj)
                return;

            Camera cam = controller.cameraObject;
            if (null != activeCamera)
            {
                activeCamera.GetComponent<CameraController>().cameraObject.gameObject.SetActive(false);
            }

            activeCamera = obj;
            cam.gameObject.SetActive(true);
            TriggerCurrentCameraChanged();
        }

        static void SetCameraEnabled(GameObject obj, bool value)
        {
            Camera cam = obj.GetComponentInChildren<Camera>(true);
            if (cam)
            {
                cam.gameObject.SetActive(value);
            }
        }

        static void UpdateCurrentObjectOutline()
        {
            if (outlinedObject)
            {
                RemoveFromHover(outlinedObject);
                outlinedObject = null;
            }

            if (grippedObject)
            {
                outlinedObject = grippedObject;
            }
            else
            {
                if (hoveredObject)
                {
                    outlinedObject = hoveredObject;
                    VRInput.SendHapticImpulse(VRInput.rightController, 0, 0.1f, 0.1f);
                }
            }

            if (outlinedObject)
                AddToHover(outlinedObject);
        }

        public static GameObject GetHoveredObject()
        {
            return hoveredObject;
        }

        public static void SetHoveredObject(GameObject obj)
        {                    
            if (obj == null || !IsSelected(obj))
            {
                hoveredObject = obj;
            }
            UpdateCurrentObjectOutline();
        }

        public static GameObject GetGrippedObject()
        {
            return grippedObject;
        }

        public static void SetGrippedObject(GameObject obj)
        {
            grippedObject = obj;
            UpdateCurrentObjectOutline();
            TriggerGrippedObjectChanged();
        }

        public static List<GameObject> GetObjects()
        {
            List<GameObject> gameObjects = new List<GameObject>();
            if (grippedObject && !IsSelected(grippedObject))
            {
                gameObjects.Add(grippedObject);
            }
            else
            {
                foreach (GameObject obj in selection.Values)
                    gameObjects.Add(obj);
            }
            return gameObjects;
        }


        public static bool IsHandleSelected()
        {
            bool handleSelected = false;
            List<GameObject> objects = GetObjects();

            if (objects.Count == 1)
            {
                foreach (GameObject obj in objects)
                {
                    if (obj.GetComponent<UIHandle>())
                        handleSelected = true;
                }
            }
            return handleSelected;
        }

        public static bool AddToHover(GameObject gObject)
        {
            if (gObject)
            {
                UIUtils.SetRecursiveLayer(gObject, "Hover");
                CameraController controller = gObject.GetComponent<CameraController>();
                if (null != controller) { SetActiveCamera(controller); }
            }

            return true;
        }

        public static CameraController GetSelectedCamera()
        {
            foreach (GameObject selectedItem in selection.Values)
            {
                CameraController controller = selectedItem.GetComponent<CameraController>();
                if (null != controller)
                    return controller;
            }

            return null;
        }

        public static bool RemoveFromHover(GameObject gObject)
        {
            string layerName = "Default";

            if (selection.ContainsKey(gObject.GetInstanceID()))
            {
                layerName = "Selection";
            }
            else if (gObject.GetComponent<LightController>()
                    || gObject.GetComponent<CameraController>()
                    || gObject.GetComponent<UIHandle>())
            {
                layerName = "UI";
            }

            if (gObject)
            {
                UIUtils.SetRecursiveLayer(gObject, layerName);
            }

            CameraController controller = gObject.GetComponent<CameraController>();
            if (null != controller)
            {
                controller = GetSelectedCamera();
                if (null != controller)
                    SetActiveCamera(controller);
            }

            return true;
        }

        public static bool AddToSelection(GameObject gObject)
        {
            if (gObject.GetComponent<UIHandle>())
                return false;

            if (selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Add(gObject.GetInstanceID(), gObject);

            CameraController controller = gObject.GetComponent<CameraController>();
            if (null != controller) { SetActiveCamera(controller); }

            UIUtils.SetRecursiveLayer(gObject, "Selection");

            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }

            return true;
        }

        public static bool RemoveFromSelection(GameObject gObject)
        {
            if (!selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Remove(gObject.GetInstanceID());


            string layerName = "Default";
            if (gObject.GetComponent<LightController>()
             || gObject.GetComponent<CameraController>()
             || gObject.GetComponent<UIHandle>())
            {
                layerName = "UI";
            }

            UIUtils.SetRecursiveLayer(gObject, layerName);

            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }

            return true;
        }

        public static void ClearSelection()
        {
            foreach (KeyValuePair<int, GameObject> data in selection)
            {
                string layerName = "Default";
                if (data.Value.GetComponent<LightController>()
                 || data.Value.GetComponent<CameraController>()
                 || data.Value.GetComponent<UIHandle>())
                {
                    layerName = "UI";
                }

                UIUtils.SetRecursiveLayer(data.Value, layerName);
            }

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Clear();

            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }
        }
    }
}
