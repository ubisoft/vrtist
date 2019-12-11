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
        public float hueToSaturationRatio = 0.12f;
        public float hueToPreviewRatio = 0.88f;
        public Material saturationMaterial = null;
        public Material hueMaterial = null;
        public Material previewMaterial = null;
        public GameObject saturationCursorPrefab = null;
        public GameObject hueCursorPrefab = null;

        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.3f;
        private static readonly float default_thickness = 0.001f;
        private static readonly float default_padding = 0.01f;
        private static readonly float default_hueToSaturationRatio = 0.12f;
        private static readonly float default_hueToPreviewRatio = 0.88f;

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
            if (T != null)
            {
                parent = T;
            }

            UIColorPicker.CreateUIColorPicker(
                "ColorPicker", parent, Vector3.zero,
                default_width, default_height, default_thickness, default_padding,
                default_hueToSaturationRatio, default_hueToPreviewRatio,
                UIUtils.LoadMaterial("Saturation"), UIUtils.LoadMaterial("Hue"), UIUtils.LoadMaterial("Preview"),
                LoadDefaultSaturationCursor(), LoadDefaultHueCursor());
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UIColorPicker";

            if (saturationMaterial == null)
            {
                saturationMaterial = UIUtils.LoadMaterial("Saturation");
            }

            if (hueMaterial == null)
            {
                hueMaterial = UIUtils.LoadMaterial("Hue");
            }

            if (previewMaterial == null)
            {
                previewMaterial = UIUtils.LoadMaterial("Preview");
            }
        }

        static GameObject LoadDefaultSaturationCursor()
        {
            return Resources.Load("Prefabs/UI/Cursor_Saturation") as GameObject;
        }

        static GameObject LoadDefaultHueCursor()
        {
            return Resources.Load("Prefabs/UI/Cursor_Hue") as GameObject;
        }

        private void OnWizardCreate()
        {
            UIColorPicker.CreateUIColorPicker(
                widgetName, parentPanel ? parentPanel.transform : null, Vector3.zero, 
                width, height, thickness, padding,
                hueToSaturationRatio, hueToPreviewRatio,
                saturationMaterial, hueMaterial, previewMaterial,
                saturationCursorPrefab, hueCursorPrefab);
        }
    }
}
