using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class WizardCreateUIKeyView : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UIKeyView", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIKeyView.Create(new UIKeyView.CreateParams 
            { 
                parent = parent
            });
        }
    }
}
