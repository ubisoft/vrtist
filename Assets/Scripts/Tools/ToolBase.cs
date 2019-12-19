using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public abstract class ToolBase : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] protected Transform panel = null;

        static GameObject previousTool = null;
        protected bool switchToSelectionEnabled = true;

        private bool isInGui = false;
        public bool IsInGui { get { return isInGui; } set { isInGui = value; ShowTool(!value); } }

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
                            ToolsUIManager.Instance.ChangeTool(previousTool.name);
                            previousTool = null;
                        }
                        // Go to selection mode
                        else
                        {
                            if (ToolsManager.Instance.currentToolRef.name != "Selector")
                            {
                                previousTool = ToolsManager.Instance.currentToolRef;
                            }
                            ToolsUIManager.Instance.ChangeTool("Selector");
                        }
                    });
                }

                /*
                ToolsUIManager.Instance.UpdateProxy3D();
                */
                // Custom tool update
                DoUpdate(position, rotation); // call children DoUpdate
                if (IsInGui)
                {
                    DoUpdateGui();
                }
            }
        }

        protected abstract void DoUpdate(Vector3 position, Quaternion rotation);
        protected virtual void DoUpdateGui() { }
        protected virtual void ShowTool(bool show) { }

        public virtual void OnUIObjectEnter(int gohash) { }
        public virtual void OnUIObjectExit(int gohash) { }
    }
}