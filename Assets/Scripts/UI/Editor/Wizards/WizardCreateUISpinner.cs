using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUISpinner : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UISpinner", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UISpinner.Create(new UISpinner.CreateArgs
            {
                parent = parent
            });
        }
    }
}
