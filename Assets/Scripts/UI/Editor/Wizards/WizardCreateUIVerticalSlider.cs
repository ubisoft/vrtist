using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUIVerticalSlider : ScriptableWizard
    {
        private static readonly string default_slider_name = "VerticalSlider";
        private static readonly float default_width = 0.025f;
        private static readonly float default_height = 0.15f;
        private static readonly float default_margin = 0.002f;
        private static readonly float default_thickness = 0.001f;
        private static readonly float default_slider_begin = 0.0f;//0.2f;
        private static readonly float default_slider_end = 0.85f;//0.9f;
        private static readonly float default_rail_margin = 0.002f;
        private static readonly float default_rail_thickness = 0.0005f;
        private static readonly float default_knob_radius = 0.0065f;
        private static readonly float default_knob_depth = 0.0025f;
        private static readonly float default_min_value = 0.0f;
        private static readonly float default_max_value = 1.0f;
        private static readonly float default_current_value = 0.5f;
        private static readonly Material default_background_material = null;
        private static readonly Material default_rail_material = null;
        private static readonly Material default_knob_material = null;
        private static readonly Color default_background_color = UIElement.default_color;
        private static readonly Color default_rail_color = UIElement.default_slider_rail_color;
        private static readonly Color default_knob_color = UIElement.default_slider_knob_color;
        private static readonly string default_text = "Slider";
        private static readonly Sprite default_icon = null; // use LoadDefault...

        public UIPanel parentPanel = null;
        public string sliderName = default_slider_name;
        public float width = default_width;
        public float height = default_height;
        public float margin = default_margin;
        public float thickness = default_thickness;
        public float slider_begin = default_slider_begin;
        public float slider_end = default_slider_end;
        public float rail_margin = default_rail_margin;
        public float rail_thickness = default_rail_thickness;
        public float knob_radius = default_knob_radius;
        public float knob_depth = default_knob_depth;
        public float min_value = default_min_value;
        public float max_value = default_max_value;
        public float current_value = default_current_value;
        public Material uiBackgroundMaterial = default_background_material;
        public Material uiRailMaterial = default_rail_material;
        public Material uiKnobMaterial = default_knob_material;
        public Color backgroundColor = default_background_color;
        public Color railColor = default_rail_color;
        public Color knobColor = default_knob_color;
        public string caption = default_text;
        public Sprite icon = default_icon;

        [MenuItem("VRtist/Create UI Vertical Slider")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUISlider>("Create UI Vertical Slider", "Create");
        }

        [MenuItem("GameObject/VRtist/UIVerticalSlider", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIVerticalSlider.Create(default_slider_name, parent,
                Vector3.zero, default_width, default_height, default_margin, default_thickness, default_slider_begin, default_slider_end,
                default_rail_margin, default_rail_thickness, default_knob_radius, default_knob_depth, 
                default_min_value, default_max_value, default_current_value,
                UIUtils.LoadMaterial("UIBase"), UIUtils.LoadMaterial("UISliderRail"), UIUtils.LoadMaterial("UISliderKnob"),
                default_background_color, default_rail_color, default_knob_color,
                default_text, UIUtils.LoadIcon("paint"));
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UIVerticalSlider";

            if (uiBackgroundMaterial == null)
            {
                uiBackgroundMaterial = UIUtils.LoadMaterial("UIBase");
            }

            if (uiRailMaterial == null)
            {
                uiRailMaterial = UIUtils.LoadMaterial("UISliderRail");
            }

            if (uiKnobMaterial == null)
            {
                uiKnobMaterial = UIUtils.LoadMaterial("UISliderKnob");
            }

            if (icon == null)
            {
                icon = UIUtils.LoadIcon("paint");
            }
        }

        private void OnWizardCreate()
        {
            UIVerticalSlider.Create(sliderName, parentPanel ? parentPanel.transform : null, Vector3.zero, 
                width, height, margin, thickness, slider_begin, slider_end, rail_margin, rail_thickness, knob_radius, knob_depth,
                min_value, max_value, current_value, 
                uiBackgroundMaterial, uiRailMaterial, uiKnobMaterial,
                backgroundColor, railColor, knobColor,
                caption, icon);
        }
    }
}
