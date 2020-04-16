using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorTrigger : MonoBehaviour
    {
        public SelectorBase selector = null;

        private bool selectionHasChanged = false;
        private HashSet<GameObject> collidedObjects = new HashSet<GameObject>();
        private GameObject lastCollidedObject = null;
        private bool grippedGameObject = false;
        private bool multiSelecting = false;

        private Color highlightColorOffset = new Color(0.4f, 0.4f, 0.4f);

        protected CommandGroup undoGroup = null;

        private void OnDisable()
        {
            if(null !=undoGroup)
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
                    grippedGameObject = false;
                    multiSelecting = false;
                    lastCollidedObject = null;
                }
                if (null != undoGroup)
                {
                    undoGroup.Submit();
                    undoGroup = null;
                }
            });

            int count = collidedObjects.Count;
            if (count > 0)
            {
                switch (selector.mode)
                {
                    case SelectorBase.SelectorModes.Select: UpdateSelection(lastCollidedObject); break;
                    case SelectorBase.SelectorModes.Eraser: UpdateEraser(lastCollidedObject); break;
                }
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "PhysicObject")
            {
                int oldCount = collidedObjects.Count;
                collidedObjects.Add(other.gameObject);
                int newCount = collidedObjects.Count;

                if(newCount != oldCount)
                {
                    lastCollidedObject = other.gameObject;
                }

                gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selector.GetModeColor() + highlightColorOffset);
                selector.OnSelectorTriggerEnter(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.tag == "PhysicObject")
            {
                collidedObjects.Remove(other.gameObject);
                if(collidedObjects.Count == 0) {
                    gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selector.GetModeColor());
                }
                selector.OnSelectorTriggerExit(other);
            }
        }

        private void UpdateSelection(GameObject gObject)
        {
            // Get right controller buttons states
            bool primaryButtonState = VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton);
            bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
            bool gripState = VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton);

            // Mono-selection using the grip button
            if (!grippedGameObject && gripState && !triggerState && !primaryButtonState && !multiSelecting && !GlobalState.IsGrippingWorld)
            {
                selector.ClearSelection();
                selector.AddSiblingsToSelection(gObject);
                collidedObjects.Clear();
                selectionHasChanged = true;
                lastCollidedObject = null;
                grippedGameObject = true;
            }

            if(grippedGameObject && !gripState)
            {
                grippedGameObject = false;
            }
            else if (triggerState)  // Multi-selection using the trigger button
            {
                if (!primaryButtonState)
                {
                    selector.AddSiblingsToSelection(gObject);
                    collidedObjects.Clear();
                    grippedGameObject = false;
                    selectionHasChanged = true;
                    multiSelecting = true;
                }
                else
                {
                    selector.RemoveSiblingsFromSelection(gObject);
                    collidedObjects.Clear();
                    grippedGameObject = false;
                    selectionHasChanged = true;
                    multiSelecting = true;
                }
            }
        }

        private void UpdateEraser(GameObject gObject)
        {
            // Don't delete UI handles
            if (gObject.GetComponent<UIHandle>())
                return;

            if (VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton))
            {
                selector.RemoveSiblingsFromSelection(gObject, false);
                collidedObjects.Clear();
                grippedGameObject = false;

                // Add a selectionVFX instance on the deleted object
                GameObject vfxInstance = Instantiate(selector.selectionVFXPrefab);
                vfxInstance.GetComponent<SelectionVFX>().SpawnDeleteVFX(gObject);

                VRInput.SendHapticImpulse(VRInput.rightController,0, 1, 0.2f);
                new CommandRemoveGameObject(gObject).Submit();
            }
        }
    }
}
