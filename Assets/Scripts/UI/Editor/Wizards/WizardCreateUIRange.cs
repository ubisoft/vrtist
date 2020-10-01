using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUIRange : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UIRange", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIRange.Create(new UIRange.CreateArgs
            {
                parent = parent
            });
        }
    }
}
