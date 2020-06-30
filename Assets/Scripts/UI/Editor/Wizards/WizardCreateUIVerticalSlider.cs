using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUIVerticalSlider : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UIVerticalSlider", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIVerticalSlider.Create(new UIVerticalSlider.CreateArgs 
            {
                parent = parent
            });
        }
    }
}
