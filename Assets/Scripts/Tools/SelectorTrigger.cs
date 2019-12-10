using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorTrigger : MonoBehaviour
    {
        public Selector selector = null;

        [Header("UI")]
        //[SerializeField] protected ToolsUIManager uiTools;        

        const float deadZone = 0.3f;
        private bool selectionHasChanged = false;

        private List<GameObject> collidedObjects = new List<GameObject>();
        private bool grippedGameObject = false;

        private bool multiSelecting = false;
        private Color selectionColor;

        private bool hasUIObject = false;

        // Start is called before the first frame update
        void Start()
        {
            selectionColor = gameObject.GetComponent<MeshRenderer>().material.GetColor("_BaseColor");
        }

        protected CommandGroup undoGroup = null;

        void Update()
        {
            //if (uiTools.isOverUI()) { return; }

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
                undoGroup.Submit();
                undoGroup = null;
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
                gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", Color.white);
            }
            //else if (other.tag == "UIObject" && selector != null)
            //{
            //    other.gameObject.transform.localScale *= 1.1f;
            //    selector.OnUIObjectEnter(other.gameObject);
            //    gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", Color.white);
            //    hasUIObject = true;
            //}
        }

        // TODO: handle UI 3D Objects grab elsewhere.

        private void OnTriggerExit(Collider other)
        {
            //if (other.tag == "UIObject" && selector != null)
            //{
            //    other.gameObject.transform.localScale /= 1.1f;
            //    selector.OnUIObjectExit(other.gameObject);
            //    gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            //}
            //else
            {
                collidedObjects.Remove(other.gameObject);
                if(collidedObjects.Count == 0)
                    gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            }
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
            if(!grippedGameObject && gripState && !triggerState && !primaryButtonState && !multiSelecting)
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

            // Multi-selection using the trigger button
            else if (triggerState)
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
            if (VRInput.GetValue(VRInput.rightController, CommonUsages.triggerButton))
            {
                selector.RemoveSiblingsFromSelection(gObject, false);
                collidedObjects.Clear();
                grippedGameObject = false;
                VRInput.rightController.SendHapticImpulse(0, 1, 0.2f);
                Destroy(gObject);
            }
        }
    }
}