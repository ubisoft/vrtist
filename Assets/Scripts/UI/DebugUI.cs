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
        public void UIOPTIONS_Refresh()
        {
            // refresh all items
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];
                    element.NeedsRebuild = true;
                    //element.RefreshColor();
                }
            }
        }

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
                    element.baseColor.constant = UIOptions.BackgroundColor;
                    element.baseColor.reference = UIOptions.Instance.backgroundColor;

                    element.disabledColor.useConstant = false;
                    element.disabledColor.constant = UIOptions.DisabledColor;
                    element.disabledColor.reference = UIOptions.Instance.disabledColor;

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        // pushedColor
                        // checkedColor

                        if (button.transform.parent.gameObject.name == "CloseButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.CloseWindowButtonColor;
                            button.baseColor.reference = UIOptions.Instance.closeWindowButtonColor;
                        }

                        if (button.transform.parent.gameObject.name == "PinButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.PinWindowButtonColor;
                            button.baseColor.reference = UIOptions.Instance.pinWindowButtonColor;
                        }
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        label.textColor.useConstant = false;
                        label.textColor.constant = UIOptions.ForegroundColor;
                        label.textColor.reference = UIOptions.Instance.foregroundColor;
                    }

                    UIElement panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        panel.baseColor.useConstant = false;
                        panel.baseColor.constant = UIOptions.PanelColor;
                        panel.baseColor.reference = UIOptions.Instance.panelColor;
                    }

                    UIElement checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                    }

                    UIElement slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
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

        public void UIOPTIONS_RandomChangeColors()
        {
            UIOptions.BackgroundColorVar.value = Random.ColorHSV();

            UIOPTIONS_Refresh();
        }
    }
}
