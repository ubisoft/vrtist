using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Colorize : ToolBase
    {
        [SerializeField] private Transform colorizeTrigger;

        private UISlider roughnessSlider;
        private UISlider metallicSlider;

        private UIButton colorizeButton;
        private UIButton pickButton;
        private UIButton updateSelectionButton;

        private MeshRenderer previewRenderer;

        private bool uiInitialized = false;

        private enum ColorOp
        {
            Colorize,
            Pick,
            UpdateSelection
        };
        
        private ColorOp colorOp = ColorOp.Colorize;

        void Start()
        {
            Init();

            roughnessSlider = panel.Find("Roughness").gameObject.GetComponent<UISlider>();
            metallicSlider = panel.Find("Metallic").gameObject.GetComponent<UISlider>();
            colorizeButton = panel.Find("ColorizeButton").gameObject.GetComponent<UIButton>();
            pickButton = panel.Find("PickButton").gameObject.GetComponent<UIButton>();
            updateSelectionButton = panel.Find("SetSelectionButton").gameObject.GetComponent<UIButton>();
            previewRenderer = panel.Find("ColorPreview").gameObject.GetComponent<MeshRenderer>();

            // Realtime update: preview only
            roughnessSlider.onSlideEvent.AddListener((float value) => UpdatePreview());
            metallicSlider.onSlideEvent.AddListener((float value) => UpdatePreview());
            GlobalState.colorChangedEvent.AddListener((Color color) => UpdatePreview());

            // On release update
            roughnessSlider.onReleaseEvent.AddListener(UpdateMaterial);
            metallicSlider.onReleaseEvent.AddListener(UpdateMaterial);
            GlobalState.colorReleasedEvent.AddListener((Color color) => UpdateMaterial());
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            colorOp = ColorOp.Colorize;
            uiInitialized = false;
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            if(!uiInitialized)
            {
                uiInitialized = true;
                colorizeButton.Checked = (colorOp == ColorOp.Colorize);
                pickButton.Checked = (colorOp == ColorOp.Pick);
                updateSelectionButton.Checked = (colorOp == ColorOp.UpdateSelection);
                UpdatePreview();
            }
        }

        private void UpdateMaterial()
        {
            if(!gameObject.activeSelf) { return; }

            UpdatePreview();

            if(colorOp == ColorOp.UpdateSelection)
            {
                ColorizeObjects(Selection.selection.Values.ToList());
            }
        }

        private void UpdatePreview()
        {
            if(!gameObject.activeSelf) { return; }

            previewRenderer.material.SetColor("_BaseColor", GlobalState.CurrentColor);
            previewRenderer.material.SetFloat("_Smoothness", 1f - roughnessSlider.Value);
            previewRenderer.material.SetFloat("_Metallic", metallicSlider.Value);
        }

        protected override void ShowTool(bool show)
        {
            ActivateMouthpiece(colorizeTrigger, show);
            if(rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        // Called by ColorizeTrigger script
        public void ProcessObjects(List<GameObject> gobjects)
        {
            switch(colorOp)
            {
                case ColorOp.Colorize: ColorizeObjects(gobjects); break;
                case ColorOp.Pick: PickObjects(gobjects); break;
                case ColorOp.UpdateSelection: SelectObjects(gobjects); break;
            }
        }

        private void ColorizeObjects(List<GameObject> gobjects)
        {
            CommandMaterial command = new CommandMaterial(gobjects, GlobalState.CurrentColor, roughnessSlider.Value, metallicSlider.Value);
            command.Redo();
            command.Submit();
            VRInput.SendHaptic(VRInput.rightController, 0.1f, 0.2f);
        }

        private void PickObjects(List<GameObject> gobjects)
        {
            MeshRenderer renderer = gobjects[0].GetComponentInChildren<MeshRenderer>();
            GlobalState.CurrentColor = renderer.material.GetColor("_BaseColor");
            if(renderer.material.HasProperty("_Smoothness")) { roughnessSlider.Value = 1f - renderer.material.GetFloat("_Smoothness"); }
            else { roughnessSlider.Value = renderer.material.GetFloat("_Roughness"); }
            metallicSlider.Value = renderer.material.GetFloat("_Metallic");
            UpdatePreview();
            VRInput.SendHaptic(VRInput.rightController, 0.1f, 0.2f)
        }

        private void SelectObjects(List<GameObject> gobjects)
        {
            bool primaryState = VRInput.GetValue(VRInput.rightController, CommonUsages.primaryButton);
            ICommand command;
            if(!primaryState) { command = new CommandAddToSelection(gobjects); }
            else { command = new CommandRemoveFromSelection(gobjects); }
            command.Redo();
            command.Submit();
        }

        // Buttons callbacks
        public void OnSetColorize()
        {
            colorOp = ColorOp.Colorize;
            colorizeButton.Checked = true;
            pickButton.Checked = false;
            updateSelectionButton.Checked = false;
        }

        public void OnSetPick()
        {
            colorOp = ColorOp.Pick;
            colorizeButton.Checked = false;
            pickButton.Checked = true;
            updateSelectionButton.Checked = false;
        }

        public void OnSetUpdateSelection()
        {
            colorOp = ColorOp.UpdateSelection;
            colorizeButton.Checked = false;
            pickButton.Checked = false;
            updateSelectionButton.Checked = true;
            UpdateMaterial();
        }
    }
}
