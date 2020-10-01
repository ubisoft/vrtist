using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorTrigger : MonoBehaviour
    {
        public SelectorBase selector = null;

        private bool selectionHasChanged = false;
        private List<GameObject> collidedObjects = new List<GameObject>();

        private Color highlightColorOffset = new Color(0.4f, 0.4f, 0.4f);

        protected CommandGroup undoGroup = null;

        private void OnDisable()
        {
            if (null != undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }

            collidedObjects.Clear();
            Selection.SetHoveredObject(null);
        }

        void Update()
        {
            // Clear selection on trigger click on nothing
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
            {
                selectionHasChanged = false;
                undoGroup = new CommandGroup("Selector Trigger");
            },
            () =>
            {
                try
                {
                    if (!selectionHasChanged && !VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton) && !VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton))
                    {
                        selector.ClearSelection();
                    }
                }
                finally
                {
                    if (null != undoGroup)
                    {
                        undoGroup.Submit();
                        undoGroup = null;
                    }
                }
            });

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
                if (obj.name == "__Offset")
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
                    collidedObjects.Add(gObject);
                if (Selection.GetHoveredObject() != gObject)
                {
                    Selection.SetHoveredObject(gObject);
                    selector.OnSelectorTriggerEnter(other);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "PhysicObject")
            {
                GameObject gObject = GetRootIfCollectionInstance(other.gameObject);

                if (gObject == Selection.GetHoveredObject())
                {
                    selector.OnSelectorTriggerExit(other);
                }

                RemoveCollidedObject(gObject);
            }
        }

        private void RemoveCollidedObject(GameObject obj)
        {
            bool removed = collidedObjects.Remove(obj);
            if (!removed) { return; }

            GameObject hoveredObject = Selection.GetHoveredObject();
            if (hoveredObject == obj)
            {
                hoveredObject = null;
                while (collidedObjects.Count > 0)
                {
                    int index = collidedObjects.Count - 1;
                    hoveredObject = collidedObjects[index];
                    if (!Utils.IsInTrash(hoveredObject))
                        break;
                    collidedObjects.RemoveAt(index);
                    hoveredObject = null;
                }
                Selection.SetHoveredObject(hoveredObject);
            }
        }

        private void UpdateSelection()
        {
            // Get right controller buttons states
            bool primaryButtonState = VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton);
            bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
            bool gripState = VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton);

            GameObject hoveredObject = Selection.GetHoveredObject();

            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.grip,
                 () => { Selection.SetGrippedObject(hoveredObject); },
                 () => { Selection.SetGrippedObject(null); });

            // Mono-selection using the grip button
            if (triggerState && null != hoveredObject)  // Multi-selection using the trigger button
            {
                selectionHasChanged = true;
                if (!primaryButtonState)
                {
                    foreach (GameObject obj in collidedObjects)
                        selector.AddSiblingsToSelection(obj);
                }
                else
                {
                    //selector.RemoveSiblingsFromSelection(hoveredObject);
                }
            }
        }

        private void UpdateEraser()
        {
            GameObject hoveredObject = Selection.GetHoveredObject();
            if (null == hoveredObject && Selection.IsEmpty())
                return;

            // If we have a hovered object, only destroy it
            if (null != hoveredObject)
            {
                // Don't delete UI handles
                if (hoveredObject.GetComponent<UIHandle>())
                    return;

                if (VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton))
                {
                    CommandGroup group = new CommandGroup("Erase Hovered Object");
                    try
                    {
                        RemoveCollidedObject(hoveredObject);
                        selector.RemoveSiblingsFromSelection(hoveredObject, false);

                        ToolsUIManager.Instance.SpawnDeleteInstanceVFX(hoveredObject);

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
                if (VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton))
                {
                    CommandGroup group = new CommandGroup("Erase Selected Objects");
                    try
                    {
                        foreach (GameObject gobject in Selection.GetSelectedObjects())
                        {
                            RemoveCollidedObject(gobject);
                            selector.RemoveSiblingsFromSelection(gobject, false);

                            ToolsUIManager.Instance.SpawnDeleteInstanceVFX(gobject);

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
