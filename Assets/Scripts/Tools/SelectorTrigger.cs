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
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () => {
                selectionHasChanged = false;
                undoGroup = new CommandGroup("Selector Trigger");
            },
            () => {
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "PhysicObject")
            {
                if(!collidedObjects.Contains(other.gameObject))
                    collidedObjects.Add(other.gameObject);
                if (Selection.GetHoveredObject() != other.gameObject)
                {
                    Selection.SetHoveredObject(other.gameObject);
                    selector.OnSelectorTriggerEnter(other);
                }
            }
        }

        private void RemoveCollidedObject(GameObject obj)
        {
            collidedObjects.Remove(obj);
            GameObject hoveredObject = Selection.GetHoveredObject();
            if(hoveredObject == obj)
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

        private void OnTriggerExit(Collider other)
        {
            if(other.tag == "PhysicObject")
            {
                if (other.gameObject == Selection.GetHoveredObject())
                {
                    selector.OnSelectorTriggerExit(other);
                }

                RemoveCollidedObject(other.gameObject);
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
                    selector.AddSiblingsToSelection(hoveredObject);
                }
                else
                {
                    selector.RemoveSiblingsFromSelection(hoveredObject);
                }
            }
        }

        private void UpdateEraser()
        {
            GameObject hoveredObject = Selection.GetHoveredObject();
            if (null == hoveredObject)
                return;
            // Don't delete UI handles
            if (hoveredObject.GetComponent<UIHandle>())
                return;

            if (VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton))
            {
                CommandGroup group = new CommandGroup("Erase Selection");
                try
                {
                    RemoveCollidedObject(hoveredObject);
                    selector.RemoveSiblingsFromSelection(hoveredObject, false);

                    // Add a selectionVFX instance on the deleted object
                    GameObject vfxInstance = Instantiate(selector.selectionVFXPrefab);
                    vfxInstance.GetComponent<SelectionVFX>().SpawnDeleteVFX(hoveredObject);

                    VRInput.SendHapticImpulse(VRInput.rightController, 0, 1, 0.2f);
                    new CommandRemoveGameObject(hoveredObject).Submit();
                }
                finally
                {
                    group.Submit();
                }
            }
        }
    }
}
