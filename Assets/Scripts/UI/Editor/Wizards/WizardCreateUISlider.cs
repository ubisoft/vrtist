using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUISlider : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UISlider", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UISlider.Create(new UISlider.CreateArgs
            {
                parent = parent
            });
        }
    }
}
