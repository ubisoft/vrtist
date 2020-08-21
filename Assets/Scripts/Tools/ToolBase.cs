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

        protected Transform rightHandle;
        protected Transform rightMouthpiece;
        protected Transform rightController;

        private bool hasListener = false;

        protected virtual void Awake()
        {
            ToolsManager.RegisterTool(gameObject);
        }

        protected virtual void Init()
        {
            rightController = transform.parent.parent.Find("right_controller");
            UnityEngine.Assertions.Assert.IsNotNull(rightController);
            rightHandle = rightController.parent;
            UnityEngine.Assertions.Assert.IsNotNull(rightHandle);
            rightMouthpiece = rightHandle.Find("mouthpieces");
            UnityEngine.Assertions.Assert.IsNotNull(rightMouthpiece);
        }

        protected void ActivateMouthpiece(Transform mouthPiece, bool activate)
        {
            Transform container = mouthPiece.parent;
            for(int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                child.gameObject.SetActive(activate && child == mouthPiece);
            }
        }

        void Start()
        {
            Init();
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // Device rotation
                Vector3 position;
                Quaternion rotation;
                VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);
                rightHandle.localPosition = position;
                rightHandle.localRotation = rotation;
                /*
                rightController.localPosition = position;
                rightController.localRotation = rotation;
                transform.localPosition = position;
                transform.localRotation = rotation;
                */
                Vector3 r = rotation.eulerAngles;

                // Toggle selection
                if (enableToggleTool)
                {
                    VRInput.ButtonEvent(VRInput.rightController, CommonUsages.secondaryButton, () =>
                    {
                        ToolsManager.ToggleTool();
                    });
                }

                // if tool has switch, THIS is now disabled, we dont want updates.
                if (!gameObject.activeSelf)
                    return;

                // Custom tool update
                if (IsInGui)
                {
                    DoUpdateGui();
                }
                else // TODO: voir si il faut pas quand meme faire DoUpdate dans tous les cas.
                // le probleme de faire les deux vient quand ils reagissent au meme input (ex: Grip dans UI)
                {
                    DoUpdate(); // call children DoUpdate
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

        protected virtual void OnEnable()
        {
            GlobalState.Instance.AddAnimationListener(OnParametersChanged);
            hasListener = true;
        }

        protected virtual void OnDisable()
        {
            if(hasListener && null != GlobalState.Instance)
                GlobalState.Instance.RemoveAnimationListener(OnParametersChanged);
            hasListener = false;
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
            if(!gameObject.activeSelf) { return; }
            if (null != parameterCommand)
            {
                parameterCommand.Submit();
                parameterCommand = null;
            }
        }
        protected abstract void DoUpdate();
        protected virtual void DoUpdateGui() { }
        protected virtual void ShowTool(bool show) { }

        public virtual void OnUIObjectEnter(int gohash) { }
        public virtual void OnUIObjectExit(int gohash) { }
    }
}