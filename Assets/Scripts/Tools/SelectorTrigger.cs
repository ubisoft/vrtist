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

        public void SetLastCollidedObject(GameObject gobject)
        {
            lastCollidedObject = gobject;
        }

        public GameObject GetLastCollidedObject()
        {
            return lastCollidedObject;
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
                    //lastCollidedObject = null;
                }
                if (null != undoGroup)
                {
                    undoGroup.Submit();
                    undoGroup = null;
                }
            });

            switch (selector.mode)
            {
                case SelectorBase.SelectorModes.Select: UpdateSelection(lastCollidedObject); break;
                case SelectorBase.SelectorModes.Eraser: UpdateEraser(lastCollidedObject); break;
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

                Selection.SetHoveredObject(other.gameObject);

                selector.OnSelectorTriggerEnter(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.tag == "PhysicObject")
            {
                if (other.gameObject == lastCollidedObject)
                {
                    lastCollidedObject = null;
                }

                collidedObjects.Remove(other.gameObject);
                if(collidedObjects.Count == 0) 
                {
                    gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selector.GetModeColor());
                }

                // TODO: guard to ensure enter/exit
                if(Selection.GetHoveredObject() == other.gameObject)
                    Selection.SetHoveredObject(null);

                selector.OnSelectorTriggerExit(other);
            }
        }

        private void UpdateSelection(GameObject gObject)
        {
            // Get right controller buttons states
            bool primaryButtonState = VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton);
            bool triggerState = VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton);
            bool gripState = VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton);

            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.grip,
                 () => { Selection.SetGrippedObject(lastCollidedObject); },
                 () => { Selection.SetGrippedObject(null); });

            // Mono-selection using the grip button
            if (triggerState)  // Multi-selection using the trigger button
            {
                if (!primaryButtonState)
                {
                    if(null != gObject) { selector.AddSiblingsToSelection(gObject); }
                    collidedObjects.Clear();
                    selectionHasChanged = true;
                }
                else
                {
                    if(null != gObject) { selector.RemoveSiblingsFromSelection(gObject); }
                    collidedObjects.Clear();
                    selectionHasChanged = true;
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

                // Add a selectionVFX instance on the deleted object
                GameObject vfxInstance = Instantiate(selector.selectionVFXPrefab);
                vfxInstance.GetComponent<SelectionVFX>().SpawnDeleteVFX(gObject);

                VRInput.SendHapticImpulse(VRInput.rightController,0, 1, 0.2f);
                new CommandRemoveGameObject(gObject).Submit();
            }
        }
    }
}
