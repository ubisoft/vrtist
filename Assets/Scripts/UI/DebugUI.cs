using UnityEngine;

namespace VRtist
{
    public class DebugUI : MonoBehaviour
    {
        public UIDynamicList shotList = null;

        public UIHandle[] windows = null;

        //
        // Shot List
        //
        public void SHOTLIST_AddItemToList(Transform t)
        {
            if (shotList != null)
            {
                shotList.AddItem(t);
            }
        }

        public void SHOTLIST_ClearList()
        {
            if (shotList != null)
            {
                shotList.Clear();
            }
        }

        //
        // UIOptions
        //
        public void UIOPTIONS_ResetAllColors()
        {
            for(int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    //element.baseColor.useConstant = true;
                    //element.baseColor.constant = Color.gray;

                    element.baseColor.useConstant = false;
                    element.baseColor.constant = UIOptions.Instance.backgroundColor.value;
                    element.baseColor.reference = UIOptions.Instance.backgroundColor;

                    element.disabledColor.useConstant = false;
                    element.disabledColor.constant = UIOptions.Instance.disabledColor.value;
                    element.disabledColor.reference = UIOptions.Instance.disabledColor;

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        // pushedColor
                        // checkedColor
                    }

                    //UILabel label = element.GetComponent<UILabel>();
                    UIElement panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        panel.baseColor.useConstant = true;
                        panel.baseColor.constant = Color.black;
                    }

                    UIElement checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        checkbox.baseColor.useConstant = true;
                        checkbox.baseColor.constant = Color.green;
                    }

                    UIElement slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        slider.baseColor.useConstant = true;
                        slider.baseColor.constant = Color.red;
                    }

                    //UIElement vslider = element.GetComponent<UIVerticalSlider>();
                    //UIElement spinner = element.GetComponent<UISpinner>();
                    //UIElement timebar = element.GetComponent<UITimeBar>();

                    element.NeedsRebuild = true;
                    //element.RefreshColor();
                }
            }
        }
    }
}
