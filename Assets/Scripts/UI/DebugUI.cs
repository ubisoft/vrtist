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
                    //element.ResetColor();
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

                    element.pushedColor.useConstant = false;
                    element.pushedColor.constant = UIOptions.PushedColor;
                    element.pushedColor.reference = UIOptions.PushedColorVar;

                    element.selectedColor.useConstant = false;
                    element.selectedColor.constant = UIOptions.SelectedColor;
                    element.selectedColor.reference = UIOptions.SelectedColorVar;

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        // CheckedColor
                        button.checkedColor.useConstant = false;
                        button.checkedColor.constant = UIOptions.CheckedColor;
                        button.checkedColor.reference = UIOptions.CheckedColorVar;

                        // Text Color
                        button.textColor.useConstant = false;
                        button.textColor.constant = UIOptions.ForegroundColor;
                        button.textColor.reference = UIOptions.ForegroundColorVar;

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

                        if (button.gameObject.name == "ExitButton")
                        {
                            button.baseColor.useConstant = false;
                            button.baseColor.constant = UIOptions.ExitButtonColor;
                            button.baseColor.reference = UIOptions.ExitButtonColorVar;
                        }
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        // Label TextColor
                        label.textColor.useConstant = false;
                        label.textColor.constant = UIOptions.ForegroundColor;
                        label.textColor.reference = UIOptions.ForegroundColorVar;
                    }

                    UIElement panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        // Specific base color for Panels
                        panel.baseColor.useConstant = false;
                        panel.baseColor.constant = UIOptions.PanelColor;
                        panel.baseColor.reference = UIOptions.PanelColorVar;
                    }

                    UIGrabber grabber = element.GetComponent<UIGrabber>();
                    if (grabber)
                    {
                        grabber.baseColor.useConstant = false;
                        grabber.baseColor.constant = UIOptions.GrabberBaseColor;
                        grabber.baseColor.reference = UIOptions.GrabberBaseColorVar;

                        grabber.pushedColor.useConstant = false;
                        grabber.pushedColor.constant = UIOptions.GrabberHoverColor;
                        grabber.pushedColor.reference = UIOptions.GrabberHoverColorVar;

                        SerializedObject so = new SerializedObject(grabber);
                        so.FindProperty("baseColor").FindPropertyRelative("useConstant").boolValue = true;
                        so.FindProperty("baseColor").FindPropertyRelative("constant").colorValue = UIOptions.GrabberBaseColor;
                        //so.FindProperty("baseColor").FindPropertyRelative("reference").objectReferenceValue = UIOptions.GrabberBaseColorVar;

                        so.FindProperty("pushedColor").FindPropertyRelative("useConstant").boolValue = true;
                        so.FindProperty("pushedColor").FindPropertyRelative("constant").colorValue = UIOptions.GrabberHoverColor;
                        //so.FindProperty("pushedColor").FindPropertyRelative("reference").objectReferenceValue = UIOptions.GrabberHoverColorVar;
                        so.ApplyModifiedProperties();
                    }

                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                    }

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        // TODO: knob and rail
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        // TODO: knob and rail
                    }

                    UISpinner spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {
                    }

                    UITimeBar timebar = element.GetComponent<UITimeBar>();
                    if (timebar)
                    {
                    }

                    element.NeedsRebuild = true;
                    //element.ResetColor();
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
#if UNITY_EDITOR
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
#endif
        }

        //
        //
        //

        public void Checkable_SetBaseSprite()
        {
#if UNITY_EDITOR
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
#endif
        }


        public void MATERIALS_RelinkAndFix()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        button.source_material = UIUtils.LoadMaterial(UIButton.default_material_name);
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        label.source_material = UIUtils.LoadMaterial(UILabel.default_material_name);
                    }

                    UIPanel panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        panel.source_material = UIUtils.LoadMaterial(UIPanel.default_material_name);
                    }

                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        checkbox.source_material = UIUtils.LoadMaterial(UICheckbox.default_material_name);
                    }

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        slider.sourceMaterial = UIUtils.LoadMaterial(UISlider.default_material_name);
                        slider.sourceKnobMaterial = UIUtils.LoadMaterial(UISlider.default_rail_material_name);
                        slider.sourceRailMaterial = UIUtils.LoadMaterial(UISlider.default_knob_material_name);
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        vslider.sourceMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_material_name);
                        vslider.sourceKnobMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_rail_material_name);
                        vslider.sourceRailMaterial = UIUtils.LoadMaterial(UIVerticalSlider.default_knob_material_name);
                    }

                    UISpinner spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {
                        spinner.sourceMaterial = UIUtils.LoadMaterial(UISpinner.default_background_material_name);
                    }

                    // These UIElements do not have source_material yet


                    //UITimeBar timebar = element.GetComponent<UITimeBar>();
                    //if (timebar)
                    //{
                    //    timebar.source_material = UIUtils.LoadMaterial(UIPanel.default_material_name);
                    //}

                    //UIColorPickerHue colorpickerhue = element.GetComponent<UIColorPickerHue>();
                    //if (colorpickerhue)
                    //{
                    //    colorpickerhue.source_material = UIUtils.LoadMaterial(UIPanel.default_material_name);
                    //}

                    //UIColorPickerSaturation colorpickersat = element.GetComponent<UIColorPickerSaturation>();
                    //if (colorpickersat)
                    //{
                    //    colorpickersat.source_material = UIUtils.LoadMaterial(UIPanel.default_material_name);
                    //}

                    //UIColorPickerPreview colorpickerprev = element.GetComponent<UIColorPickerPreview>();
                    //if (colorpickerprev)
                    //{
                    //    colorpickerprev.source_material = UIUtils.LoadMaterial(UIPanel.default_material_name);
                    //}

                    element.ResetMaterial();
                }
            }
        }

        // TODO: script to copy all Text from the Text component to the TextArea of UIButtons
        // --> done in Update of buttons.
    }
}
