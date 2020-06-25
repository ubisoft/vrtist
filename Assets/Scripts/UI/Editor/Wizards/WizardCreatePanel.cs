using UnityEditor;
using UnityEngine;

namespace VRtist
{

    public class WizardCreatePanel : ScriptableWizard
    {
        private static readonly float default_width = 0.4f;
        private static readonly float default_height = 0.6f;
        private static readonly float default_margin = 0.02f;
        private static readonly float default_radius = 0.01f;
        private static readonly float default_thickness = 0.001f;
        private static readonly UIPanel.BackgroundGeometryStyle default_bg_geom_style = UIPanel.BackgroundGeometryStyle.Tube;

        public UIPanel parentPanel = null;
        public string panelName = "New Panel";
        public float width = default_width;
        public float height = default_height;
        public float margin = default_margin;
        public float radius = default_radius;
        public float thickness = default_thickness;
        public UIPanel.BackgroundGeometryStyle backgroundGeometryStyle = default_bg_geom_style;
        public Material uiMaterial = null;
        public Color color = UIElement.default_background_color;

        [MenuItem("GameObject/VRtist/UIPanel", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIPanel.Create("New Panel", parent, Vector3.zero, default_width, default_height, default_margin, default_radius,default_thickness, default_bg_geom_style, UIUtils.LoadMaterial("UIPanel"), UIElement.default_background_color);
        }
    }
}
