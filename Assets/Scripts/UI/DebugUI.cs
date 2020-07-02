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

                        if (button.transform.parent.gameObject.name == "CloseButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.Instance.closeWindowButtonColor.value;
                            button.baseColor.reference = UIOptions.Instance.closeWindowButtonColor;
                        }

                        if (button.transform.parent.gameObject.name == "PinButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.Instance.pinWindowButtonColor.value;
                            button.baseColor.reference = UIOptions.Instance.pinWindowButtonColor;
                        }
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        label.textColor.useConstant = false;
                        label.textColor.constant = UIOptions.Instance.foregroundColor.value;
                        label.textColor.reference = UIOptions.Instance.foregroundColor;
                    }

                    UIElement panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        panel.baseColor.useConstant = false;
                        panel.baseColor.constant = UIOptions.Instance.panelColor.value;
                        panel.baseColor.reference = UIOptions.Instance.panelColor;
                    }

                    UIElement checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        //checkbox.baseColor.useConstant = true;
                        //checkbox.baseColor.constant = Color.green;
                    }

                    UIElement slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        //slider.baseColor.useConstant = true;
                        //slider.baseColor.constant = Color.red;
                    }

                    UIElement vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {

                    }

                    UIElement spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {

                    }

                    UIElement timebar = element.GetComponent<UITimeBar>();
                    if (timebar)
                    {

                    }

                    element.NeedsRebuild = true;
                    //element.RefreshColor();
                }
            }
        }
    }
}
