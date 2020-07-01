using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUITimeBar : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UITimeBar", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UITimeBar.Create(new UITimeBar.CreateArgs 
            {
                parent = parent
            });
        }
    }
}
