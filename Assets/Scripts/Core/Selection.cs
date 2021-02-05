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


    /// <summary>
    /// The current selection state.
    /// We manage two distinct selections, a primary one which is pretty standard containing a set of objects.
    /// And an auxiliary one containing only one object that we can manipulate without changing the primary selection.
    /// We also define a set of active objects which are objects that are selected or auxiliary selected.
    /// And we also keep track of the current hovered object, which may be selected or not.
    /// </summary>
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

        // Active objects represents manipulated objects (selected or auxiliary selected objects)
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

        public static void ClearAll()
        {
            ClearSelection();
            AuxiliarySelection = null;
            HoveredObject = null;
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
}
