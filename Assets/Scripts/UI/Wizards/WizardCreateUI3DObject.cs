using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUI3DObject : ScriptableWizard
    {
        private static readonly string default_object_name = "3D Object";
        private static readonly float default_width = 0.05f;
        private static readonly float default_height = 0.05f;
        private static readonly float default_depth = 0.05f;
        
        public UIPanel parentPanel = null;
        public string objectName = default_object_name;
        public float width = default_width;
        public float height = default_height;
        public float depth = default_depth;
        public GameObject objectPrefab = null;

        [MenuItem("VRtist/Create UI 3D Object")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUI3DObject>("Create UI 3D Object", "Create");
        }

        [MenuItem("GameObject/VRtist/UI3DObject", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UI3DObject.CreateUI3DObject(default_object_name, parent, Vector3.zero, default_width, default_height, default_depth, UIUtils.LoadPrefab("LightBulb_Simple"));
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UI 3DObject";

            if (objectPrefab == null)
            {
                objectPrefab = UIUtils.LoadPrefab("LightBulb_Simple");
            }
        }

        private void OnWizardCreate()
        {
            UI3DObject.CreateUI3DObject(objectName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, depth, objectPrefab);
        }
    }
}
