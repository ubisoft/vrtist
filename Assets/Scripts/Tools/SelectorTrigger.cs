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
            collidedObjects.Clear();
            Selection.SetHoveredObject(null);

            if (null !=undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }
        }

        void Update()
        {
            // Clear selection on trigger click on nothing
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () => {
                selectionHasChanged = false;
                undoGroup = new CommandGroup();
            },
            () => {
                if (!selectionHasChanged && !VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton) && !VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton))
                {
                    selector.ClearSelection();
                }
                if (null != undoGroup)
                {
                    undoGroup.Submit();
                    undoGroup = null;
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

        private void OnTriggerExit(Collider other)
        {
            if(other.tag == "PhysicObject")
            {
                collidedObjects.Remove(other.gameObject);
                GameObject hoveredObject = Selection.GetHoveredObject();
                if (other.gameObject == hoveredObject)
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
                    selector.OnSelectorTriggerExit(other);
                }                
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
                selector.RemoveSiblingsFromSelection(hoveredObject, false);

                // Add a selectionVFX instance on the deleted object
                GameObject vfxInstance = Instantiate(selector.selectionVFXPrefab);
                vfxInstance.GetComponent<SelectionVFX>().SpawnDeleteVFX(hoveredObject);

                VRInput.SendHapticImpulse(VRInput.rightController,0, 1, 0.2f);
                new CommandRemoveGameObject(hoveredObject).Submit();
            }
        }
    }
}
