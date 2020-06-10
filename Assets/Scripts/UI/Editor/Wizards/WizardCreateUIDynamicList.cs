using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class WizardCreateUIDynamicList : ScriptableWizard
    {
        public UIPanel parentPanel = null;
        public string listName = "List";
        public float width = 0.4f;
        public float height = 0.6f;
        public float margin = 0.02f;
        public float itemWidth = 0.1f;
        public float itemHeight = 0.1f;
        public float itemDepth = 0.1f;

        private static readonly float default_width = 0.4f;
        private static readonly float default_height = 0.6f;
        private static readonly float default_margin = 0.02f;
        private static readonly float default_item_width = 0.1f;
        private static readonly float default_item_height = 0.1f;
        private static readonly float default_item_depth = 0.1f;

        [MenuItem("VRtist/Create UI Dynamic List")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<WizardCreateUIDynamicList>("Create UI Dynamic List", "Create");
        }

        [MenuItem("GameObject/VRtist/UIDynamicList", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIDynamicList.Create("List", parent, Vector3.zero, default_width, default_height, default_margin, default_item_width, default_item_height, default_item_depth);
        }

        private void OnWizardUpdate()
        {
            helpString = "Create a new UIDynamicList";
        }

        private void OnWizardCreate()
        {
            UIDynamicList.Create(listName, parentPanel ? parentPanel.transform : null, Vector3.zero, width, height, margin, itemWidth, itemHeight, itemDepth);
        }
    }
}
