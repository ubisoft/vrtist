using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUIButton : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UIButton", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = parent
            });
        }
    }
}
