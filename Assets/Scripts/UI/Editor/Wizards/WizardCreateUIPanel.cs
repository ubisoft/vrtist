using UnityEditor;
using UnityEngine;

namespace VRtist
{

    public class WizardCreateUIPanel : ScriptableWizard
    {
        [MenuItem("GameObject/VRtist/UIPanel", false, 49)]
        public static void OnCreateFromHierarchy()
        {
            Transform parent = null;
            Transform T = UnityEditor.Selection.activeTransform;
            if (T != null)
            {
                parent = T;
            }

            UIPanel.Create(new UIPanel.CreatePanelParams
            {
                parent = parent
            });
        }
    }
}
