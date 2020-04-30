using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUITimeBar : ScriptableWizard
    {
        private static readonly string default_slider_name = "TimeBar";
        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.03f;
        private static readonly float default_thickness = 0.001f;
        private static readonly int default_min_value = 0;
        private static readonly int default_max_value = 250;
        private static readonly int default_current_value = 0;
        private static readonly Material default_background_material = null;
        private static readonly Color default_background_color = UIElement.default_background_color;
        private static readonly string default_text = "TimeBar";

        public UIPanel parentPanel = null;
        public string sliderName = default_slider_name;
        public float width = default_width;
        public float height = default_height;
        public float thickness = default_thickness;
        public int min_value = default_min_value;
        public int max_value = default_max_value;
        public int current_value = default_current_value;
        public Material uiBackgroundMaterial = default_background_material;
        public Color backgroundColor = default_background_color;
        public string caption = default_text;

        [MenuItem("VRtist/Create UI TimeBar")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUITimeBar>("Create UI TimeBar", "Create");
        }

        [MenuItem("GameObject/VRtist/UITimeBar", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UITimeBar.Create(default_slider_name, parent,
                Vector3.zero, default_width, default_height, default_thickness, default_min_value, default_max_value, default_current_value,
                UIUtils.LoadMaterial("UIPanel"), default_background_color, default_text);
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UITimeBar";

            if (uiBackgroundMaterial == null)
            {
                uiBackgroundMaterial = UIUtils.LoadMaterial("UIPanel");
            }
        }

        private void OnWizardCreate()
        {
            UITimeBar.Create(sliderName, parentPanel ? parentPanel.transform : null, Vector3.zero, 
                width, height, thickness,
                min_value, max_value, current_value, 
                uiBackgroundMaterial, backgroundColor, caption);
        }
    }
}
