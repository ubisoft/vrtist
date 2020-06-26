using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUILabel : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UILabel", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UILabel.Create(parent);
        }
    }
}
