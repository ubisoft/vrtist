using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorTrigger : MonoBehaviour
    {
        public Selector selector = null;

        const float deadZone = 0.3f;
        private bool selectionHasChanged = false;

        private List<GameObject> collidedObjects = new List<GameObject>();
        private bool grippedGameObject = false;

        private bool multiSelecting = false;
        private Color highlightColorOffset = new Color(0.4f, 0.4f, 0.4f);

        private bool hasUIObject = false;

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
                GameObject collidedObject = collidedObjects[count - 1];
                switch (selector.mode)
                {
                    case Selector.SelectorModes.Select: UpdateSelection(collidedObject); break;
                    case Selector.SelectorModes.Eraser: UpdateEraser(collidedObject); break;
                }
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "PhysicObject")
            {
                collidedObjects.Add(other.gameObject);
                gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selector.GetModeColor() + highlightColorOffset);
                selector.OnSelectorTriggerEnter(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            collidedObjects.Remove(other.gameObject);
            if (collidedObjects.Count == 0)
                gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selector.GetModeColor());
            selector.OnSelectorTriggerExit(other);
        }

        private void UpdateSelection(GameObject gObject)
        {
            // get rightPrimaryState
            bool primaryButtonState = VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton);
            bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
            bool gripState = VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton);

            if(!gripState && hasUIObject)
            {
                hasUIObject = false;
            }

            if (hasUIObject) return;

            // Mono-selection using the grip button
            if (!grippedGameObject && gripState && !triggerState && !primaryButtonState && !multiSelecting
             && !GlobalState.IsGrippingWorld)
            {
                selector.ClearSelection();
                selector.AddSiblingsToSelection(gObject);
                collidedObjects.Clear();
                selectionHasChanged = true;
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

                VRInput.rightController.SendHapticImpulse(0, 1, 0.2f);
                new CommandRemoveGameObject(gObject).Submit();
            }
        }
    }
}