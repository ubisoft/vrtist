using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
        private static readonly Color default_color = UIElement.default_color;
        private static readonly string default_text = "Button";
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

            UIButton.CreateUIButton(default_button_name, parent, 
                Vector3.zero, default_width, default_height, default_margin, default_thickness, 
                LoadDefaultUIMaterial(), default_color, default_text, LoadDefaultIcon());
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

        static Sprite LoadDefaultIcon()
        {
            //Sprite sprite = Resources.Load("Textures/UI/paint") as Sprite;
            //return sprite;

            string[] pathList = AssetDatabase.FindAssets("paint", new[] { "Assets/Resources/Textures/UI" });
            if (pathList.Length > 0)
            {
                foreach (string path in pathList)
                {
                    var obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(path), typeof(Sprite));
                    if (obj is Sprite)
                        return obj as Sprite;
                }
            }
            return null;
        }

        private void OnWizardCreate()
        {
            UIButton.CreateUIButton(buttonName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, margin, thickness, uiMaterial, color, caption, icon);
        }

        //private void OnWizardOtherButton()
        //{
        //}
    }
}
