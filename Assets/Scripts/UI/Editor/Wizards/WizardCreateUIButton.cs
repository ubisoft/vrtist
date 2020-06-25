using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUIButton : ScriptableWizard
    {
        private static readonly string default_button_name = "Button";
        private static readonly float default_width = 0.15f;
        private static readonly float default_height = 0.05f;
        private static readonly float default_margin = 0.005f;
        private static readonly float default_thickness = 0.001f;
        private static readonly Material default_material = null; // use LoadDefault...
        private static readonly Color default_color = UIElement.default_background_color;
        private static readonly string default_text = "New Button";
        private static readonly Sprite default_icon = null; // use LoadDefault...

        public UIPanel parentPanel = null;
        public string buttonName = default_button_name;
        public float width = default_width;
        public float height = default_height;
        public float margin = default_margin;
        public float thickness = default_thickness;
        public Material uiMaterial = default_material;
        public Color color = default_color;
        public string caption = default_text;
        public Sprite icon = default_icon;

        [MenuItem("GameObject/VRtist/UIButton", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIButton.CreateUIButton(default_button_name, parent, 
                new Vector3(0,0,-default_thickness), default_width, default_height, default_margin, default_thickness, 
                UIUtils.LoadMaterial("UIPanel"), default_color, default_text, UIUtils.LoadIcon("paint"));
        }
    }
}
