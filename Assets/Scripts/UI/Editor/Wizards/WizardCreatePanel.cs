using UnityEditor;
using UnityEngine;

namespace VRtist
{

    public class WizardCreatePanel : ScriptableWizard
    {
        private static readonly string default_panel_name = "New Panel";
        private static readonly float default_width = 0.4f;
        private static readonly float default_height = 0.6f;
        private static readonly float default_margin = 0.02f;
        private static readonly float default_radius = 0.01f;
        private static readonly float default_thickness = 0.001f;
        private static readonly UIPanel.BackgroundGeometryStyle default_bg_geom_style = UIPanel.BackgroundGeometryStyle.Tube;

        [MenuItem("GameObject/VRtist/UIPanel", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIPanel.Create("New Panel", parent, new Vector3(0, 0, -default_thickness), default_width, default_height, default_margin, default_radius,default_thickness, default_bg_geom_style, UIUtils.LoadMaterial("UIPanel"), UIElement.default_background_color);
        }
    }
}
