using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUILabel : ScriptableWizard
    {
        private static readonly string default_label_name = "Label";
        private static readonly float default_width = 0.15f;
        private static readonly float default_height = 0.05f;
        private static readonly float default_margin = 0.005f;
        private static readonly Material default_material = null; // use LoadDefault...
        private static readonly Color default_background_color = UIElement.default_background_color;
        private static readonly Color default_foreground_color = UIElement.default_color;
        private static readonly string default_text = "Label";
        private static readonly string default_material_name = "UIElementTransparent";

        public UIPanel parentPanel = null;
        public string labelName = default_label_name;
        public float width = default_width;
        public float height = default_height;
        public float margin = default_margin;
        public Material uiMaterial = default_material;
        public Color background_color = default_background_color;
        public Color foreground_color = default_foreground_color;
        public string caption = default_text;

        [MenuItem("VRtist/Create UI Label")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUILabel>("Create UI Label", "Create");
        }

        [MenuItem("GameObject/VRtist/UILabel", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UILabel.CreateUILabel(default_label_name, parent, 
                Vector3.zero, default_width, default_height, default_margin, 
                UIUtils.LoadMaterial(default_material_name), default_background_color, default_foreground_color, default_text);
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UILabel";

            if (uiMaterial == null)
            {
                uiMaterial = UIUtils.LoadMaterial(default_material_name);
            }
        }

        private void OnWizardCreate()
        {
            UILabel.CreateUILabel(labelName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, margin, uiMaterial, background_color, foreground_color, caption);
        }
    }
}
