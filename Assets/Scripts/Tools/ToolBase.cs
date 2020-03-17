using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public abstract class ToolBase : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] protected Transform panel = null;

        // Does this tool authorizes the swap-to-alt-tool operation. 
        protected bool enableToggleTool = true;

        // State that is TRUE if the tool is inside a GUI volume.
        private bool isInGui = false;
        public bool IsInGui { get { return isInGui; } set { isInGui = value; ShowTool(!value); } }

        protected ICommand parameterCommand = null;
        protected List<ParametersController> connectedObjects = new List<ParametersController>();

        protected virtual void Awake()
        {
            ToolsManager.RegisterTool(gameObject);
        }

        void Start()
        {

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
                if (enableToggleTool)
                {
                    VRInput.ButtonEvent(VRInput.rightController, CommonUsages.secondaryButton, () =>
                    {
                        ToolsManager.ToggleTool();
                    });
                }

                // Custom tool update
                if (IsInGui)
                {
                    DoUpdateGui();
                }
                else // TODO: voir si il faut pas quand meme faire DoUpdate dans tous les cas.
                // le probleme de faire les deux vient quand ils reagissent au meme input (ex: Grip dans UI)
                {
                    DoUpdate(position, rotation); // call children DoUpdate
                }
            }
        }

        protected virtual void UpdateUI()
        {

        }

        protected virtual void OnParametersChanged(GameObject gObject)
        {
            if (Selection.IsSelected(gObject))
                UpdateUI();
        }

        protected void ClearListeners()
        {
            foreach (ParametersController parameterController in connectedObjects)
            {
                parameterController.RemoveListener(OnParametersChanged);
            }
            connectedObjects.Clear();
        }

        protected void AddListener(ParametersController parametersController)
        {
            parametersController.AddListener(OnParametersChanged);
            connectedObjects.Add(parametersController);
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            ClearListeners();
        }

        protected void OnSliderPressed(string title, string parameterPath)
        {
            parameterCommand = new CommandSetValue<float>(title, parameterPath);
        }

        protected void OnCheckboxPressed(string title, string parameterPath)
        {
            parameterCommand = new CommandSetValue<bool>(title, parameterPath);
        }
        protected void OnColorPressed(string title, string parameterPath)
        {
            parameterCommand = new CommandSetValue<Color>(title, parameterPath);
        }
        public void OnReleased()
        {
            if (null != parameterCommand)
            {
                parameterCommand.Submit();
                parameterCommand = null;
            }
        }
        protected abstract void DoUpdate(Vector3 position, Quaternion rotation);
        protected virtual void DoUpdateGui() { }
        protected virtual void ShowTool(bool show) { }

        public virtual void OnUIObjectEnter(int gohash) { }
        public virtual void OnUIObjectExit(int gohash) { }
    }
}