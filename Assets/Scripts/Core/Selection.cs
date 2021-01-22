using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public enum LayerType
    {
        Selection,
        Default,
        Hover
    }

    public class SelectionChangedArgs : EventArgs
    {
        public Dictionary<int, GameObject> selectionBefore = new Dictionary<int, GameObject>();
    }

    [Serializable]
    public class SelectionChangedEvent : UnityEvent<HashSet<GameObject>, HashSet<GameObject>>
    {
        // Empty
    }

    [Serializable]
    public class HoverChangedEvent : UnityEvent<GameObject, GameObject>
    {
        // Empty
    }

    [Serializable]
    public class AuxiliaryChangedEvent : UnityEvent<GameObject, GameObject>
    {
        // Empty
    }

    public class ActiveCameraChangedArgs : EventArgs
    {
        public GameObject activeCamera = null;
    }

    public enum SelectionType
    {
        Selection = 1,
        Hovered = 2,
        Gripped = 4,
        All = 7,
    }

    public class Selection
    {
        // Current selected objects (blue outlined)
        static readonly HashSet<GameObject> selectedObjects = new HashSet<GameObject>();
        public static HashSet<GameObject> SelectedObjects
        {
            get { return selectedObjects; }
        }
        public static SelectionChangedEvent onSelectionChanged = new SelectionChangedEvent();

        // Current hovered object (yellow outlined)
        static GameObject hoveredObject = null;
        public static GameObject HoveredObject
        {
            get { return hoveredObject; }
            set
            {
                GameObject previousHovered = hoveredObject;
                hoveredObject = value;
                onHoveredChanged.Invoke(previousHovered, value);
            }
        }
        public static HoverChangedEvent onHoveredChanged = new HoverChangedEvent();

        // An auxiliary selection represents a volatile object out of the main selection
        static HashSet<GameObject> auxiliarySelection = new HashSet<GameObject>();
        public static GameObject AuxiliarySelection
        {
            get
            {
                foreach (GameObject o in auxiliarySelection)
                {
                    return o;
                }
                return null;
            }
            set
            {
                if (IsSelected(value))
                    return;

                GameObject oldAuxiliarySelection = null;
                foreach (GameObject o in auxiliarySelection)
                {
                    oldAuxiliarySelection = o;
                    break;
                }
                auxiliarySelection.Clear();
                if (null != value)
                    auxiliarySelection.Add(value);
                onAuxiliarySelectionChanged.Invoke(oldAuxiliarySelection, value);
            }
        }
        public static AuxiliaryChangedEvent onAuxiliarySelectionChanged = new AuxiliaryChangedEvent();

        // Active objects represents manipulated objects (selected or off-selection gripped object)
        public static HashSet<GameObject> ActiveObjects
        {
            get
            {
                if (auxiliarySelection.Count == 0)
                    return selectedObjects;
                return auxiliarySelection;
            }
        }

        static int selectionStateTimestamp = 0;
        public static int SelectionStateTimestamp
        {
            get { return selectionStateTimestamp; }
        }

        public static bool IsSelected(GameObject gObject)
        {
            if (null == gObject)
                return false;
            return selectedObjects.Contains(gObject);
        }

        public static bool HasSelectedObjects()
        {
            return selectedObjects.Count != 0;
        }

        public static bool AddToSelection(GameObject gObject)
        {
            if (IsSelected(gObject))
                return false;
            selectionStateTimestamp++;
            HashSet<GameObject> previousSelectedObjects = new HashSet<GameObject>(selectedObjects);
            selectedObjects.Add(gObject);
            onSelectionChanged.Invoke(previousSelectedObjects, selectedObjects);
            return true;
        }

        public static bool RemoveFromSelection(GameObject gObject)
        {
            if (!IsSelected(gObject))
                return false;
            selectionStateTimestamp++;
            HashSet<GameObject> previousSelectedObjects = new HashSet<GameObject>(selectedObjects);
            selectedObjects.Remove(gObject);
            onSelectionChanged.Invoke(previousSelectedObjects, selectedObjects);
            return true;
        }

        public static void ClearSelection()
        {
            if (!HasSelectedObjects())
                return;
            selectionStateTimestamp++;
            HashSet<GameObject> previousSelectedObjects = new HashSet<GameObject>(selectedObjects);
            selectedObjects.Clear();
            onSelectionChanged.Invoke(previousSelectedObjects, selectedObjects);
        }

        public static bool IsHovered(GameObject gObject)
        {
            return gObject == hoveredObject;
        }

        public static bool HasHoveredObject()
        {
            return hoveredObject != null;
        }
    }

    /// <summary>
    /// Current selection and hover object.
    /// </summary>
    /*
    public class Selection
    {
        public static Color SelectedColor = new Color(0f / 255f, 167f / 255f, 255f / 255f);
        public static Color UnselectedColor = Color.white;

        public static int selectionStateTimestamp = 0;
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
            FillSelection(ref args.selectionBefore);
            OnSelectionChanged?.Invoke(null, args);
        }

        public static void TriggerGrippedObjectChanged()
        {
            GameObjectArgs args = new GameObjectArgs { gobject = grippedObject };
            OnGrippedObjectChanged?.Invoke(null, args);
        }

        public static void TriggerCurrentCameraChanged()
        {
            ActiveCameraChangedArgs args = new ActiveCameraChangedArgs
            {
                activeCamera = activeCamera
            };
            OnActiveCameraChanged?.Invoke(null, args);
        }

        public static void FillSelection(ref Dictionary<int, GameObject> s)
        {
            foreach (KeyValuePair<int, GameObject> data in selection)
                s[data.Key] = data.Value;
        }

        public static bool IsSelected(GameObject gObject)
        {
            if (null == gObject)
                return false;
            return selection.ContainsKey(gObject.GetInstanceID());
        }

        public static void SetActiveCamera(CameraController controller)
        {
            // Set no active camera
            if (null == controller)
            {
                if (null != activeCamera)
                {
                    controller = activeCamera.GetComponent<CameraController>();
                    activeCamera = null;
                    controller.UpdateCameraPreviewInFront(false);
                    controller.cameraObject.gameObject.SetActive(false);
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

            controller.UpdateCameraPreviewInFront(true);

            TriggerCurrentCameraChanged();
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
                    VRInput.SendHapticImpulse(VRInput.primaryController, 0, 0.1f, 0.1f);
                }
            }

            if (outlinedObject && !IsSelected(outlinedObject))
                AddToHoverLayer(outlinedObject);
        }

        public static GameObject GetHoveredObject()
        {
            return hoveredObject;
        }

        public static void SetHoveredObject(GameObject obj)
        {
            // obj == null
            if (null == obj)
            {
                if (hoveredObject != obj)
                {
                    hoveredObject = obj;
                    UpdateCurrentObjectOutline();
                    TriggerSelectionChanged();
                }
                return;
            }

            // obj is selected => reset hoveredObject
            if (hoveredObject != null && IsSelected(obj))
            {
                hoveredObject = null;
                UpdateCurrentObjectOutline();
                TriggerSelectionChanged();
                return;
            }

            // not selected, hover it
            if (hoveredObject != obj && !IsSelected(obj))
            {
                hoveredObject = obj;
                UpdateCurrentObjectOutline();
                TriggerSelectionChanged();
            }
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

        public static List<GameObject> GetSelectedObjects(SelectionType selectionType = SelectionType.Selection)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            if (0 != (selectionType & SelectionType.Selection))
            {
                gameObjects.InsertRange(0, selection.Values);
            }
            if (0 != (selectionType & SelectionType.Gripped))
            {
                if (null != grippedObject && !gameObjects.Contains(grippedObject))
                    gameObjects.Add(grippedObject);
            }
            if (0 != (selectionType & SelectionType.Hovered))
            {
                if (null != hoveredObject && !gameObjects.Contains(hoveredObject))
                    gameObjects.Add(hoveredObject);
            }
            return gameObjects;
        }

        public static GameObject GetFirstSelectedObject()
        {
            if (selection.Count == 0) { return null; }
            foreach (var first in selection.Values)
            {
                return first;
            }
            return null;
        }

        public static List<GameObject> GetGrippedOrSelection()
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
            List<GameObject> objects = GetGrippedOrSelection();

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

        public static bool AddToHoverLayer(GameObject gObject)
        {
            if (gObject)
            {
                UIUtils.SetRecursiveLayerSmart(gObject, LayerType.Hover);
                CameraController controller = gObject.GetComponent<CameraController>();
                if (null != controller)
                {
                    SetActiveCamera(controller);
                }
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
            LayerType layerType = LayerType.Default;

            Transform parent = gObject.transform;
            while (parent != null)
            {
                if (selection.ContainsKey(parent.gameObject.GetInstanceID()))
                {
                    layerType = LayerType.Selection;
                    break;
                }
                parent = parent.parent;
            }

            if (gObject)
            {
                UIUtils.SetRecursiveLayerSmart(gObject, layerType);
            }

            CameraController controller = gObject.GetComponent<CameraController>();
            if (null != controller)
            {
                controller = GetSelectedCamera();
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
            FillSelection(ref args.selectionBefore);

            selection.Add(gObject.GetInstanceID(), gObject);
            selectionStateTimestamp++;

            CameraController controller = gObject.GetComponentInChildren<CameraController>(true);
            if (null != controller)
                SetActiveCamera(controller);

            UIUtils.SetRecursiveLayerSmart(gObject, LayerType.Selection);

            OnSelectionChanged?.Invoke(null, args);

            return true;
        }

        public static bool RemoveFromSelection(GameObject gObject)
        {
            if (!selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            FillSelection(ref args.selectionBefore);

            selection.Remove(gObject.GetInstanceID());
            selectionStateTimestamp++;

            if (activeCamera != gObject)
                SetActiveCamera(null);


            UIUtils.SetRecursiveLayerSmart(gObject, LayerType.Default);

            OnSelectionChanged?.Invoke(null, args);

            return true;
        }

        public static void ClearSelection()
        {
            SelectionChangedArgs args = new SelectionChangedArgs();
            FillSelection(ref args.selectionBefore);

            selection.Clear();
            selectionStateTimestamp++;

            // must be done when selection is up to date
            foreach (GameObject obj in args.selectionBefore.Values)
            {
                UIUtils.SetRecursiveLayerSmart(obj, LayerType.Default);
            }

            if (hoveredObject != activeCamera)
                SetActiveCamera(null);

            OnSelectionChanged?.Invoke(null, args);
        }

        public static int Count()
        {
            return selection.Count;
        }

        public static bool IsEmpty()
        {
            return selection.Count == 0;
        }

        public static int GetSelectionStateTimestamp()
        {
            return selectionStateTimestamp;
        }
    }
    */
}
