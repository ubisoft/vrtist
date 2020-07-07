using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUIColorPicker : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UIColorPicker", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIColorPicker.Create(new UIColorPicker.CreateArgs 
            {
                parent = parent
            });
        }
    }
}
