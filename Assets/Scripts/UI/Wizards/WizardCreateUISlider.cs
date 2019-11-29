using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUISlider : ScriptableWizard
    {
        private static readonly string default_slider_name = "Slider";
        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.03f;
        private static readonly float default_margin = 0.005f;
        private static readonly float default_thickness = 0.001f;
        private static readonly float default_slider_begin = 0.2f;
        private static readonly float default_slider_end = 0.9f;
        private static readonly float default_min_value = 0.0f;
        private static readonly float default_max_value = 1.0f;
        private static readonly float default_current_value = 0.5f;
        private static readonly Material default_material = null; // use LoadDefault...
        private static readonly Color default_color = UIElement.default_color;
        private static readonly string default_text = "Slider";

        public UIPanel parentPanel = null;
        public string sliderName = default_slider_name;
        public float width = default_width;
        public float height = default_height;
        public float margin = default_margin;
        public float thickness = default_thickness;
        public float slider_begin = default_slider_begin;
        public float slider_end = default_slider_end;
        public float min_value = default_min_value;
        public float max_value = default_max_value;
        public float current_value = default_current_value;
        public Material uiMaterial = default_material;
        public Color color = default_color;
        public string caption = default_text;

        [MenuItem("VRtist/Create UI Slider")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUISlider>("Create UI Slider", "Create");
        }

        [MenuItem("GameObject/VRtist/UISlider", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UISlider.CreateUISlider(default_slider_name, parent,
                Vector3.zero, default_width, default_height, default_margin, default_thickness, default_slider_begin, default_slider_end,
                default_min_value, default_max_value, default_current_value,
                UIUtils.LoadMaterial("UIPanel"), default_color, default_text);
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UISlider";

            if (uiMaterial == null)
            {
                uiMaterial = UIUtils.LoadMaterial("UIPanel");
            }
        }

        private void OnWizardCreate()
        {
            UISlider.CreateUISlider(sliderName, parentPanel ? parentPanel.transform : null, Vector3.zero, 
                width, height, margin, thickness, slider_begin, slider_end,
                min_value, max_value, current_value, uiMaterial, color, caption);
        }
    }
}
