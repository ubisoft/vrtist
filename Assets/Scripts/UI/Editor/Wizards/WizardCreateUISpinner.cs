using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUISpinner : ScriptableWizard
    {
        private static readonly string default_spinner_name = "Spinner";
        private static readonly float default_width = 0.13f;
        private static readonly float default_height = 0.03f;
        private static readonly float default_margin = 0.005f;
        private static readonly float default_thickness = 0.001f;
        private static readonly float default_spinner_separation = 0.65f;
        private static readonly UISpinner.TextAndValueVisibilityType default_visibility_type = UISpinner.TextAndValueVisibilityType.ShowTextAndValue;
        private static readonly UISpinner.SpinnerValueType default_value_type = UISpinner.SpinnerValueType.Float;
        private static readonly float default_min_value_float = 0.0f;
        private static readonly float default_max_value_float = 1.0f;
        private static readonly float default_current_value_float = 0.5f;
        private static readonly float default_value_rate_float = 0.01f;
        private static readonly int default_min_value_int = 0;
        private static readonly int default_max_value_int = 10;
        private static readonly int default_current_value_int = 5;
        private static readonly float default_value_rate_int = 0.1f;
        private static readonly Material default_background_material = null;
        private static readonly Color default_background_color = UIElement.default_background_color;
        private static readonly string default_text = "Spinner";

        [MenuItem("GameObject/VRtist/UISpinner", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UISpinner.CreateUISpinner(default_spinner_name, parent,
                Vector3.zero, default_width, default_height, default_margin, default_thickness, default_spinner_separation,
                default_visibility_type, default_value_type,
                default_min_value_float, default_max_value_float, default_current_value_float, default_value_rate_float,
                default_min_value_int, default_max_value_int, default_current_value_int, default_value_rate_int,
                UIUtils.LoadMaterial("UIPanel"), default_background_color, 
                default_text);
        }
    }
}
