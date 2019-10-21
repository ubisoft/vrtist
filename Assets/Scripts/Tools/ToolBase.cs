using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public abstract class ToolBase : MonoBehaviour
    {
        [Header("UI")]
        //[SerializeField] protected ToolsUIManager uiTools = null;
        [SerializeField] protected Transform panel = null;

        static GameObject previousTool = null;
        protected bool switchToSelectionEnabled = true;

        protected virtual void Awake()
        {
            // default is NOT current tool
            ToolsManager.Instance.registerTool(gameObject, false);
        }

        private void Start()
        {
            if(panel == null)
            {
                Debug.LogError("Panel must be set!");
            }
            /*
            if(uiTools == null)
            {
                Debug.LogError("uiTools must be set to 'Camera Rig > Pivot > leftHandle > UI'");
            }
            */
        }

        // Update is called once per frame
        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // Device rotation
                Vector3 position;
                Quaternion rotation;
                VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);
                transform.localPosition = position;
                transform.localRotation = rotation;
                Vector3 r = rotation.eulerAngles;

                // Toggle selection
                if (switchToSelectionEnabled)
                {
                    VRInput.ButtonEvent(VRInput.rightController, CommonUsages.secondaryButton, () =>
                    {
                        // Leave selection mode
                        if (ToolsManager.Instance.currentToolRef.name == "Selector" && previousTool != null)
                        {
                            //uiTools.ChangeTool(previousTool.name);
                            previousTool = null;
                        }
                        // Go to selection mode
                        else
                        {
                            if (ToolsManager.Instance.currentToolRef.name != "Selector")
                            {
                                previousTool = ToolsManager.Instance.currentToolRef;
                            }
                            //uiTools.ChangeTool("Selector");
                        }
                    });
                }

                //uiTools.UpdateProxy3D();
                // Custom tool update
                DoUpdate(position, rotation); // call children DoUpdate
            }
        }

        protected abstract void DoUpdate(Vector3 position, Quaternion rotation);

        public virtual void OnUIObjectEnter(GameObject gObject) { }
        public virtual void OnUIObjectExit(GameObject gObject) { }
    }
}