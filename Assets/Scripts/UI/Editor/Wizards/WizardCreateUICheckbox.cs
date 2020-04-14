using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUICheckbox : ScriptableWizard
    {
        private static readonly string default_checkbox_name = "Checkbox";
        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.05f;
        private static readonly float default_margin = 0.005f;
        private static readonly float default_thickness = 0.001f;
        private static readonly Material default_material = null; // use LoadDefault...
        private static readonly Color default_color = UIElement.default_background_color;
        private static readonly string default_text = "Checkbox";
        private static readonly Sprite default_checked_icon = null; // use LoadDefault...
        private static readonly Sprite default_unchecked_icon = null; // use LoadDefault...

        public UIPanel parentPanel = null;
        public string checkboxName = default_checkbox_name;
        public float width = default_width;
        public float height = default_height;
        public float margin = default_margin;
        public float thickness = default_thickness;
        public Material uiMaterial = default_material;
        public Color color = default_color;
        public string caption = default_text;
        public Sprite checked_icon = default_checked_icon;
        public Sprite unchecked_icon = default_unchecked_icon;

        [MenuItem("VRtist/Create UI Checkbox")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUICheckbox>("Create UI Checkbox", "Create");
        }

        [MenuItem("GameObject/VRtist/UICheckbox", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UICheckbox.CreateUICheckbox(default_checkbox_name, parent,
                Vector3.zero, default_width, default_height, default_margin, default_thickness,
                UIUtils.LoadMaterial("UIPanel"), default_color, default_text, 
                UIUtils.LoadIcon("checkbox_checked"), UIUtils.LoadIcon("checkbox_unchecked"));
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UICheckbox";

            if (uiMaterial == null)
            {
                uiMaterial = UIUtils.LoadMaterial("UIPanel");
            }
        }

        private void OnWizardCreate()
        {
            UICheckbox.CreateUICheckbox(checkboxName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, margin, thickness, uiMaterial, color, caption, checked_icon, unchecked_icon);
        }
    }
}
