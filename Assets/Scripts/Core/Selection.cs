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
        public static bool enabled = true;

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
                if (!enabled) { return; }
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
                if (!enabled) { return; }
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
            if (!enabled) { return false; }
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
            if (!enabled) { return false; }
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
            if (!enabled) { return; }
            if (!HasSelectedObjects())
                return;
            selectionStateTimestamp++;
            HashSet<GameObject> previousSelectedObjects = new HashSet<GameObject>(selectedObjects);
            selectedObjects.Clear();
            onSelectionChanged.Invoke(previousSelectedObjects, selectedObjects);
        }

        public static void Clear()
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
