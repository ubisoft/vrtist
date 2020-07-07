using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class DebugUI : MonoBehaviour
    {
        public UIDynamicList shotList = null;

        public UIHandle[] windows = null;

        public GameObject[] assetBankPages = null;
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

                    element.baseColor.useConstant = false;
                    element.baseColor.constant = UIOptions.BackgroundColor;
                    element.baseColor.reference = UIOptions.BackgroundColorVar;

                    element.disabledColor.useConstant = false;
                    element.disabledColor.constant = UIOptions.DisabledColor;
                    element.disabledColor.reference = UIOptions.DisabledColorVar;

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        // pushedColor
                        // checkedColor

                        if (button.transform.parent.gameObject.name == "CloseButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.CloseWindowButtonColor;
                            button.baseColor.reference = UIOptions.CloseWindowButtonColorVar;
                        }

                        if (button.transform.parent.gameObject.name == "PinButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.PinWindowButtonColor;
                            button.baseColor.reference = UIOptions.PinWindowButtonColorVar;
                        }
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        label.textColor.useConstant = false;
                        label.textColor.constant = UIOptions.ForegroundColor;
                        label.textColor.reference = UIOptions.ForegroundColorVar;
                    }

                    UIElement panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        panel.baseColor.useConstant = false;
                        panel.baseColor.constant = UIOptions.PanelColor;
                        panel.baseColor.reference = UIOptions.PanelColorVar;
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

        //
        // Asset Bank
        //

        public void AssetBank_Reorder()
        {
            float startx = 0.05f;
            float starty = -0.08f;
            float startz = -0.025f;

            float offsetx = 0.075f;
            float offsety = -0.07f;

            for (int p = 0; p < assetBankPages.Length; ++p)
            {
                float currentx = startx;
                float currenty = starty;

                GameObject page = assetBankPages[p];
                for (int i = 0; i < page.transform.childCount; ++i)
                {
                    Transform assetTransform = page.transform.GetChild(i);
                    UIGrabber grabber = assetTransform.GetComponent<UIGrabber>();
                    if (grabber != null)
                    {
                        currentx = startx + (float)(i % 4) * offsetx;
                        currenty = starty + (float)(i / 4) * offsety;
                        SerializedObject so = new SerializedObject(grabber);
                        so.FindProperty("relativeLocation").vector3Value = new Vector3(currentx, currenty, startz);
                        so.ApplyModifiedProperties();
                        grabber.NeedsRebuild = true;
                    }
                }
            }
        }

        //
        //
        //

        public void Checkable_SetBaseSprite()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null && button.baseSprite == null)
                    {
                        Image img = button.GetComponentInChildren<Image>();
                        if (img != null && img.sprite != null)
                        {
                            //button.baseSprite = img.sprite;

                            SerializedObject so = new SerializedObject(button);
                            so.FindProperty("baseSprite").objectReferenceValue = img.sprite;
                            so.ApplyModifiedProperties();

                            element.NeedsRebuild = true;
                        }
                    }
                }
            }
        }
    }
}
