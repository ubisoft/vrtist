using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUICheckbox : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UICheckbox", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UICheckbox.Create(new UICheckbox.CreateParams
            {
                parent = parent
            });
        }
    }
}
