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

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorTrigger : MonoBehaviour
    {
        public SelectorBase selector = null;
        private readonly List<GameObject> collidedObjects = new List<GameObject>();

        private void OnDisable()
        {
            collidedObjects.Clear();
            Selection.HoveredObject = null;
        }

        void Update()
        {
            switch (selector.mode)
            {
                case SelectorBase.SelectorModes.Select: UpdateSelection(); break;
                case SelectorBase.SelectorModes.Eraser: UpdateEraser(); break;
            }
        }

        GameObject GetRootIfCollectionInstance(GameObject gObject)
        {
            if (null == gObject)
                return null;

            Transform obj = gObject.transform.parent;
            while (null != obj)
            {
                if (obj.name == Utils.blenderCollectionInstanceOffset)
                {
                    return GetRootIfCollectionInstance(obj.parent.gameObject);
                }
                obj = obj.parent;
            }
            return gObject;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "PhysicObject")
            {
                GameObject gObject = GetRootIfCollectionInstance(other.gameObject);

                if (!collidedObjects.Contains(gObject))
                {
                    collidedObjects.Add(gObject);
                }
                if (Selection.HoveredObject != gObject)
                {
                    // when moving object, we don't want to hover other objects
                    if (!GlobalState.Instance.selectionGripped)
                    {
                        Selection.HoveredObject = gObject;
                        selector.OnSelectorTriggerEnter(other);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "PhysicObject")
            {
                GameObject gObject = GetRootIfCollectionInstance(other.gameObject);

                if (gObject == Selection.HoveredObject)
                {
                    selector.OnSelectorTriggerExit(other);
                }

                RemoveCollidedObject(gObject);
            }
        }

        public void ClearCollidedObjects()
        {
            collidedObjects.Clear();
        }

        private void RemoveCollidedObject(GameObject obj)
        {
            collidedObjects.Remove(obj);

            // manage successive imbrication of objects
            GameObject hoveredObject = Selection.HoveredObject;
            if (hoveredObject == obj)
            {
                hoveredObject = null;
                while (collidedObjects.Count > 0)
                {
                    int index = collidedObjects.Count - 1;
                    hoveredObject = collidedObjects[index];
                    if (!SceneManager.IsInTrash(hoveredObject))
                        break;
                    collidedObjects.RemoveAt(index);
                    hoveredObject = null;
                }
                if (!GlobalState.Instance.selectionGripped)
                    Selection.HoveredObject = hoveredObject;
            }
        }

        public void OnEndGrip()
        {
            // manage successive imbrication of objects
            GameObject hoveredObject = null;
            while (collidedObjects.Count > 0)
            {
                int index = collidedObjects.Count - 1;
                hoveredObject = collidedObjects[index];
                if (!SceneManager.IsInTrash(hoveredObject))
                    break;
                collidedObjects.RemoveAt(index);
                hoveredObject = null;
            }
            Selection.HoveredObject = hoveredObject;
        }


        private void UpdateSelection()
        {
            // Get right controller buttons states
            bool primaryButtonState = VRInput.GetValue(VRInput.primaryController, CommonUsages.primaryButton);
            bool triggerState = VRInput.GetValue(VRInput.primaryController, CommonUsages.triggerButton);

            GameObject hoveredObject = Selection.HoveredObject;

            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip,
                 () =>
                 {
                     Selection.HoveredObject = hoveredObject;
                 },
                 () => { Selection.AuxiliarySelection = null; });

            // Multi-selection using the trigger button
            if (triggerState && !GlobalState.Instance.selectionGripped && null != hoveredObject && !selector.Deforming)
            {
                if (!primaryButtonState)
                {
                    foreach (GameObject obj in collidedObjects)
                        selector.AddSiblingsToSelection(obj);
                }
            }
        }

        private void UpdateEraser()
        {
            GameObject hoveredObject = Selection.HoveredObject;
            if (null == hoveredObject && Selection.SelectedObjects.Count == 0)
                return;

            // If we have a hovered object, only destroy it
            if (null != hoveredObject)
            {
                // Don't delete UI handles
                if (hoveredObject.GetComponent<UIHandle>())
                    return;

                if (VRInput.GetValue(VRInput.primaryController, CommonUsages.triggerButton))
                {
                    CommandGroup group = new CommandGroup("Erase Hovered Object");
                    try
                    {
                        RemoveCollidedObject(hoveredObject);
                        selector.RemoveSiblingsFromSelection(hoveredObject, false);

                        new CommandRemoveGameObject(hoveredObject).Submit();
                    }
                    finally
                    {
                        group.Submit();
                    }
                }
            }

            // If we don't have any hovered object but we collided with a selection, delete the whole selection
            else if (collidedObjects.Count > 0 && Selection.IsSelected(collidedObjects[0]))
            {
                if (VRInput.GetValue(VRInput.primaryController, CommonUsages.triggerButton))
                {
                    CommandGroup group = new CommandGroup("Erase Selected Objects");
                    try
                    {
                        foreach (GameObject gobject in Selection.SelectedObjects)
                        {
                            RemoveCollidedObject(gobject);
                            selector.RemoveSiblingsFromSelection(gobject, false);

                            new CommandRemoveGameObject(gobject).Submit();
                        }
                    }
                    finally
                    {
                        group.Submit();
                    }
                }
            }
        }
    }
}
