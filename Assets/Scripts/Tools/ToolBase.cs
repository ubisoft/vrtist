using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public abstract class ToolBase : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] protected Transform panel = null;

        // Does this tool authorizes the swap-to-alt-tool operation. 
        protected bool enableToggleTool = true;

        // State that is TRUE if the tool is inside a GUI volume.
        private bool isInGui = false;
        public bool IsInGui { get { return isInGui; } set { isInGui = value; ShowTool(!value); } }

        protected ICommand parameterCommand = null;

        public Transform mouthpiece;

        protected static Transform mouthpieces;

        protected virtual void Awake()
        {
            ToolsManager.RegisterTool(gameObject);
            mouthpieces = GlobalState.Instance.toolsController.Find("mouthpieces");
        }

        protected virtual void Init()
        {

        }

        public static void ToggleMouthpiece(Transform mouthPiece, bool activate)
        {
            foreach (Transform child in mouthpieces)
            {
                child.gameObject.SetActive(activate && child == mouthPiece);
            }
        }

        protected void SetControllerVisible(bool visible)
        {
            GlobalState.SetPrimaryControllerVisible(visible);
            // Mouth piece have the selectorTrigger script attached to them which has to be always enabled
            // So don't deactivate mouth piece, but hide it instead
            ShowMouthpiece(visible);
        }

        protected void ShowMouthpiece(bool value)
        {
            if (null == mouthpiece) // some tools dont have mouthpieces (WindowTool)
                return;

            foreach (var meshRenderer in mouthpiece.GetComponentsInChildren<MeshRenderer>(true))
            {
                meshRenderer.enabled = value;
            }
        }

        public void ActivateMouthpiece(bool value)
        {
            if (null == mouthpiece) // some tools dont have mouthpieces (WindowTool)
                return;

            mouthpiece.gameObject.SetActive(value);
        }

        void Start()
        {
            Init();
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // Toggle selection
                if (enableToggleTool)
                {
                    VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.secondaryButton, () =>
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

        protected virtual void OnParametersChanged(GameObject gObject, Curve curve)
        {
            if (Selection.IsSelected(gObject))
                UpdateUI();
        }

        protected virtual void OnEnable()
        {
            Settings.onSettingsChanged.AddListener(OnSettingsChanged);
            SetTooltips();
        }

        protected virtual void OnDisable()
        {
            Settings.onSettingsChanged.RemoveListener(OnSettingsChanged);
        }
        protected virtual void OnSettingsChanged()
        {
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
            if (!gameObject.activeSelf) { return; }
            if (null != parameterCommand)
            {
                parameterCommand.Submit();
                parameterCommand = null;
            }
        }
        protected abstract void DoUpdate();
        protected virtual void DoUpdateGui() { }

        protected virtual void ShowTool(bool show)
        {
            ToggleMouthpiece(mouthpiece, show);
        }

        //protected virtual void ShowController(bool show)
        //{
        //    if (rightController != null)
        //    {
        //        rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
        //    }
        //}

        public virtual void OnUIObjectEnter(int gohash) { }
        public virtual void OnUIObjectExit(int gohash) { }

        public virtual bool SubToggleTool()
        {
            return false;
        }

        public virtual void SetTooltips()
        {
            // Empty
        }
    }
}