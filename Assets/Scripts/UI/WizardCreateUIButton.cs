using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUIButton : ScriptableWizard
    {
        public UIPanel parentPanel = null;
        public string buttonName = "Button";
        public float width = 0.15f;
        public float height = 0.05f;
        public float margin = 0.005f;
        public float thickness = 0.001f;
        public Material uiMaterial = null;
        public Color color = Color.white;

        private static readonly float default_width = 0.15f;
        private static readonly float default_height = 0.05f;
        private static readonly float default_margin = 0.005f;
        private static readonly float default_thickness = 0.001f;

        [MenuItem("VRtist/Create UI Button")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUIButton>("Create UI Button", "Create");//, "OtherButton");
        }

        [MenuItem("GameObject/VRtist/UIButton", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)// && T.GetComponent<UIPanel>() != null)
            {
                parent = T;
            }

            UIButton.CreateUIButton("Button", parent, Vector3.zero, default_width, default_height, default_margin, default_thickness, LoadDefaultUIMaterial(), UIElement.default_color);
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UIButton";

            if (uiMaterial == null)
            {
                uiMaterial = LoadDefaultUIMaterial();
            }
        }

        static Material LoadDefaultUIMaterial()
        {
            string[] uiMaterialAssetPath = AssetDatabase.FindAssets("UIPanel", new[] { "Assets/Resources/Materials" });
            if (uiMaterialAssetPath.Length == 1)
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(uiMaterialAssetPath[0]), typeof(Material)) as Material;
            }
            else
            {
                return null;
            }
        }

        private void OnWizardCreate()
        {
            UIButton.CreateUIButton(buttonName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, margin, thickness, uiMaterial, color);
        }

        //private void OnWizardOtherButton()
        //{
        //}
    }
}
