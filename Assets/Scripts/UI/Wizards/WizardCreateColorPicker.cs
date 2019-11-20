using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUIColorPicker : ScriptableWizard
    {
        public UIPanel parentPanel = null;
        public string widgetName = "ColorPicker";
        public float width = 0.3f;
        public float height = 0.3f;
        public float thickness = 0.001f;
        public float padding = 0.01f;
        public Material saturationMaterial = null;
        public Material hueMaterial = null;
        public Material previewMaterial = null;

        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.3f;
        private static readonly float default_thickness = 0.001f;
        private static readonly float default_padding = 0.01f;

        [MenuItem("VRtist/Create UI ColorPicker")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUIColorPicker>("Create UI ColorPicker", "Create");
        }

        [MenuItem("GameObject/VRtist/UIColorPicker", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)// && T.GetComponent<UIPanel>() != null)
            {
                parent = T;
            }

            UIColorPicker.CreateUIColorPicker(
                "ColorPicker", parent, Vector3.zero,
                default_width, default_height, default_thickness, default_padding,
                LoadDefaultSaturationMaterial(), LoadDefaultHueMaterial(), LoadDefaultPreviewMaterial());
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UIColorPicker";

            if (saturationMaterial == null)
            {
                saturationMaterial = LoadDefaultSaturationMaterial();
            }

            if (hueMaterial == null)
            {
                hueMaterial = LoadDefaultHueMaterial();
            }

            if (previewMaterial == null)
            {
                previewMaterial = LoadDefaultPreviewMaterial();
            }
        }

        static Material LoadDefaultSaturationMaterial()
        {
            string[] uiMaterialAssetPath = AssetDatabase.FindAssets("Saturation", new[] { "Assets/Resources/Materials/UI/Color Picker" });
            if (uiMaterialAssetPath.Length == 1)
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(uiMaterialAssetPath[0]), typeof(Material)) as Material;
            }
            else
            {
                return null;
            }
        }

        static Material LoadDefaultHueMaterial()
        {
            string[] uiMaterialAssetPath = AssetDatabase.FindAssets("Hue", new[] { "Assets/Resources/Materials/UI/Color Picker" });
            if (uiMaterialAssetPath.Length == 1)
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(uiMaterialAssetPath[0]), typeof(Material)) as Material;
            }
            else
            {
                return null;
            }
        }

        static Material LoadDefaultPreviewMaterial()
        {
            string[] uiMaterialAssetPath = AssetDatabase.FindAssets("SolidColor", new[] { "Assets/Resources/Materials/UI/Color Picker" });
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
            UIColorPicker.CreateUIColorPicker(
                widgetName, parentPanel ? parentPanel.transform : null, Vector3.zero, 
                width, height, thickness, padding,
                saturationMaterial, hueMaterial, previewMaterial);
        }
    }
}
