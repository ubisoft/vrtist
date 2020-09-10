using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class DebugUI : MonoBehaviour
    {
        public UIHandle[] windows = null;

        public GameObject[] assetBankPages = null;

#if UNITY_EDITOR
        
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

        public void UIOPTIONS_ResetAllThickness()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        checkbox.thickness = 0.005f;
                    }

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        slider.thickness = 0.005f;
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        vslider.thickness = 0.005f;
                    }

                    element.NeedsRebuild = true;
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public void UIOPTIONS_ResetAllColors()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    element.baseColor.useConstant = false;
                    element.baseColor.constant = UIOptions.BackgroundColor;
                    element.baseColor.reference = UIOptions.BackgroundColorVar;

                    element.textColor.useConstant = false;
                    element.textColor.constant = UIOptions.ForegroundColor;
                    element.textColor.reference = UIOptions.ForegroundColorVar;

                    element.disabledColor.useConstant = false;
                    element.disabledColor.constant = UIOptions.DisabledColor;
                    element.disabledColor.reference = UIOptions.DisabledColorVar;

                    element.pushedColor.useConstant = false;
                    element.pushedColor.constant = UIOptions.PushedColor;
                    element.pushedColor.reference = UIOptions.PushedColorVar;

                    element.selectedColor.useConstant = false;
                    element.selectedColor.constant = UIOptions.SelectedColor;
                    element.selectedColor.reference = UIOptions.SelectedColorVar;

                    element.hoveredColor.useConstant = false;
                    element.hoveredColor.constant = UIOptions.HoveredColor;
                    element.hoveredColor.reference = UIOptions.HoveredColorVar;

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
                        label.baseColor.useConstant = false;
                        label.baseColor.constant = UIOptions.InvisibleColor;
                        label.baseColor.reference = UIOptions.InvisibleColorVar;

                        label.textColor.useConstant = false;
                        label.textColor.constant = UIOptions.ForegroundColor;
                        label.textColor.reference = UIOptions.ForegroundColorVar;

                        if (label.gameObject.name == "SectionLabel")
                        {
                            label.textColor.useConstant = false;
                            label.textColor.constant = UIOptions.SectionTextColor;
                            label.textColor.reference = UIOptions.SectionTextColorVar;
                        }

                        if (label.gameObject.name == "TitleBar")
                        {
                            label.baseColor.useConstant = false;
                            label.baseColor.constant = UIOptions.PanelColor;
                            label.baseColor.reference = UIOptions.PanelColorVar;
                        }
                    }

                    UIPanel panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        // Specific BASE color for Panels
                        panel.baseColor.useConstant = false;
                        panel.baseColor.constant = UIOptions.PanelColor;
                        panel.baseColor.reference = UIOptions.PanelColorVar;

                        // Specific HOVER color for Panels
                        panel.hoveredColor.useConstant = false;
                        panel.hoveredColor.constant = UIOptions.PanelHoverColor;
                        panel.hoveredColor.reference = UIOptions.PanelHoverColorVar;
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
                        checkbox.baseColor.useConstant = false;
                        checkbox.baseColor.constant = UIOptions.InvisibleColor;
                        checkbox.baseColor.reference = UIOptions.InvisibleColorVar;
                    }

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        slider.rail._color.useConstant = false;
                        slider.rail._color.constant = UIOptions.SliderRailColor;
                        slider.rail._color.reference = UIOptions.SliderRailColorVar;

                        slider.knob._color.useConstant = false;
                        slider.knob._color.constant = UIOptions.SliderKnobColor;
                        slider.knob._color.reference = UIOptions.SliderKnobColorVar;
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        vslider.rail._color.useConstant = false;
                        vslider.rail._color.constant = UIOptions.SliderRailColor;
                        vslider.rail._color.reference = UIOptions.SliderRailColorVar;

                        vslider.knob._color.useConstant = false;
                        vslider.knob._color.constant = UIOptions.SliderKnobColor;
                        vslider.knob._color.reference = UIOptions.SliderKnobColorVar;
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

        public void UIOPTIONS_HoveredColor()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    element.hoveredColor.useConstant = false;
                    element.hoveredColor.constant = UIOptions.HoveredColor;
                    element.hoveredColor.reference = UIOptions.HoveredColorVar;

                    UIPanel panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        // Specific base color for Panels
                        panel.hoveredColor.useConstant = false;
                        panel.hoveredColor.constant = UIOptions.PanelHoverColor;
                        panel.hoveredColor.reference = UIOptions.PanelHoverColorVar;
                    }

                        element.NeedsRebuild = true;
                    //element.ResetColor();
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
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
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public void CheckBox_SortingOrder()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        Canvas canvas = checkbox.transform.Find("Canvas").gameObject.GetComponent<Canvas>();
                        canvas.sortingOrder = 1;
                        
                        MeshRenderer r = canvas.transform.Find("Text").gameObject.GetComponent<MeshRenderer>();
                        if (r != null)
                        {
                            r.sortingOrder = 1;
                        }

                        element.NeedsRebuild = true;
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        //
        // Text Mesh Pro
        //

        private void TMP_2_TMPUI(TextMeshPro tmp, GameObject gObj)
        {

            TMPro.TextAlignmentOptions align = tmp.alignment;
            float tmin = tmp.fontSizeMin;
            float tmax = tmp.fontSizeMax;
            bool autoS = tmp.enableAutoSizing;
            TMPro.FontStyles fs = tmp.fontStyle;
            Color c = tmp.color;
            string s = tmp.text;

            DestroyImmediate(tmp);

            if (gObj.GetComponent<TextMeshProUGUI>() == null)
            {
                TextMeshProUGUI tui = gObj.AddComponent<TextMeshProUGUI>();
                tui.alignment = align;
                tui.fontSizeMin = tmin;
                tui.fontSizeMax = tmax;
                tui.enableAutoSizing = autoS;
                tui.fontStyle = fs;
                tui.color = c;
                tui.text = s;
            }
        }

        public void Replace_TextMeshPro_By_TextMeshProUGUI()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    #region button
                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        Transform textObjectTransform = button.transform.Find("Canvas/Text");

                        TextMeshPro oldText = textObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldText != null)
                        {
                            TMP_2_TMPUI(oldText, textObjectTransform.gameObject);
                        }

                        textObjectTransform.gameObject.SetActive(button.content != UIButton.ButtonContent.ImageOnly);
                    }
                    #endregion

                    #region label
                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        Transform textObjectTransform = label.transform.Find("Canvas/Text");
                        TextMeshPro oldText = label.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldText != null)
                        {
                            TMP_2_TMPUI(oldText, textObjectTransform.gameObject);
                        }
                    }
                    #endregion

                    #region checkbox
                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        Transform textObjectTransform = checkbox.transform.Find("Canvas/Text");
                        TextMeshPro oldText = textObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldText != null)
                        {
                            TMP_2_TMPUI(oldText, textObjectTransform.gameObject);
                        }
                    }
                    #endregion

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        Transform textObjectTransform = slider.transform.Find("Canvas/Text");
                        TextMeshPro oldText = textObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldText != null)
                        {
                            TMP_2_TMPUI(oldText, textObjectTransform.gameObject);
                        }

                        Transform textValueObjectTransform = slider.transform.Find("Canvas/TextValue");
                        TextMeshPro oldTextValue = textValueObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldTextValue != null)
                        {
                            TMP_2_TMPUI(oldTextValue, textValueObjectTransform.gameObject);
                        }
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        Transform textValueObjectTransform = vslider.transform.Find("Canvas/TextValue");
                        TextMeshPro oldTextValue = textValueObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldTextValue != null)
                        {
                            TMP_2_TMPUI(oldTextValue, textValueObjectTransform.gameObject);
                        }
                    }

                    UISpinner spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {
                        bool hasText = (spinner.textAndValueVisibilityType == UISpinner.TextAndValueVisibilityType.ShowTextAndValue);

                        Transform textObjectTransform = spinner.transform.Find("Canvas/Text");
                        TextMeshPro oldText = textObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldText != null)
                        {
                            TMP_2_TMPUI(oldText, textObjectTransform.gameObject);
                        }

                        textObjectTransform.gameObject.SetActive(hasText);

                        Transform textValueObjectTransform = spinner.transform.Find("Canvas/TextValue");
                        TextMeshPro oldTextValue = textValueObjectTransform.gameObject.GetComponentInChildren<TextMeshPro>(true);
                        if (oldTextValue != null)
                        {
                            TMP_2_TMPUI(oldTextValue, textValueObjectTransform.gameObject);
                        }
                    }

                    //UIPanel panel = element.GetComponent<UIPanel>();
                    //if (panel != null)
                    //{

                    //}

                    //UITimeBar timebar = element.GetComponent<UITimeBar>();
                    //if (timebar)
                    //{

                    //}

                    //UIColorPickerHue colorpickerhue = element.GetComponent<UIColorPickerHue>();
                    //if (colorpickerhue)
                    //{

                    //}

                    //UIColorPickerSaturation colorpickersat = element.GetComponent<UIColorPickerSaturation>();
                    //if (colorpickersat)
                    //{

                    //}

                    //UIColorPickerPreview colorpickerprev = element.GetComponent<UIColorPickerPreview>();
                    //if (colorpickerprev)
                    //{

                    //}

                    element.NeedsRebuild = true;
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        #region UTILS
        static void PrintObjectProperties(Object o)
        {
            SerializedObject so = new SerializedObject(o);
            SerializedProperty sp = so.GetIterator();
            do
            {
                Debug.Log($"n: {sp.name} dn: {sp.displayName} p: {sp.propertyPath}");
            } while (sp.Next(true));
        }
        #endregion

        #region TEMPLATE
        public void TEMPLATE()
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
                        
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        
                    }

                    UIPanel panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        
                    }

                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        
                    }

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        
                    }

                    UISpinner spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {
                        
                    }


                    UITimeBar timebar = element.GetComponent<UITimeBar>();
                    if (timebar)
                    {
                        
                    }

                    UIColorPickerHue colorpickerhue = element.GetComponent<UIColorPickerHue>();
                    if (colorpickerhue)
                    {
                        
                    }

                    UIColorPickerSaturation colorpickersat = element.GetComponent<UIColorPickerSaturation>();
                    if (colorpickersat)
                    {
                        
                    }

                    UIColorPickerPreview colorpickerprev = element.GetComponent<UIColorPickerPreview>();
                    if (colorpickerprev)
                    {
                        
                    }

                    element.NeedsRebuild = true;
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        #endregion

        #region DEPRECATED

        //
        // Add Colliders to UIPanel
        //

        public void AddCollidersToUIPanels()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    UIPanel panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        MeshFilter meshFilter = panel.gameObject.GetComponent<MeshFilter>();
                        if (meshFilter != null) // some panels have no geometry (containers only).
                        {
                            BoxCollider coll = panel.gameObject.GetComponent<BoxCollider>();
                            if (coll == null) // get first in cas we already clicked on the button.
                            {
                                coll = panel.gameObject.AddComponent<BoxCollider>();
                            }
                            if (coll != null && meshFilter.sharedMesh != null)
                            {
                                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                                coll.center = initColliderCenter;
                                coll.size = initColliderSize;
                                coll.isTrigger = true;
                            }
                        }
                    }

                    element.NeedsRebuild = true;
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        //
        // Text Mesh Pro
        //

        public void Replace_Text_By_TextMeshPro()
        {
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    #region button
                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        Transform textObjectTransform = button.transform.Find("Canvas/Text");

                        Text oldText = textObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        if (oldText != null)
                        {
                            DestroyImmediate(oldText);
                        }

                        if (textObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = button.textContent;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = TextAlignmentOptions.Left;
                            t.color = button.TextColor;
                        }

                        textObjectTransform.gameObject.SetActive(button.content != UIButton.ButtonContent.ImageOnly);
                    }
                    #endregion

                    #region label
                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        Text oldText = label.gameObject.GetComponentInChildren<Text>(true);
                        TMPro.TextAlignmentOptions align = TextAlignmentOptions.Left;
                        if (oldText != null)
                        {
                            if (oldText.alignment == TextAnchor.MiddleCenter)
                            {
                                align = TextAlignmentOptions.Midline;
                            }
                            else if (oldText.alignment == TextAnchor.UpperLeft)
                            {
                                align = TextAlignmentOptions.TopLeft;
                            }
                            DestroyImmediate(oldText);
                        }

                        Transform textObjectTransform = label.transform.Find("Canvas/Text");

                        if (textObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = label.textContent;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.renderer.sortingOrder = 1;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = align;
                            t.color = label.TextColor;
                        }
                    }
                    #endregion

                    #region checkbox
                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        Transform textObjectTransform = checkbox.transform.Find("Canvas/Text");

                        Text oldText = textObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        string oldTextContent = "";
                        if (oldText != null)
                        {
                            oldTextContent = oldText.text;
                            checkbox.textContent = oldTextContent; // fix empty textContent.
                            DestroyImmediate(oldText);
                        }

                        if (textObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = checkbox.textContent;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = TextAlignmentOptions.Left;
                            t.color = checkbox.TextColor;
                        }
                    }
                    #endregion

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        Transform textObjectTransform = slider.transform.Find("Canvas/Text");
                        Text oldText = textObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        string oldTextContent = "";
                        if (oldText != null)
                        {
                            oldTextContent = oldText.text;
                            slider.textContent = oldTextContent; // fix empty textContent.
                            DestroyImmediate(oldText);
                        }

                        if (textObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = slider.textContent;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = TextAlignmentOptions.Left;
                            t.color = slider.TextColor;
                        }

                        Transform textValueObjectTransform = slider.transform.Find("Canvas/TextValue");
                        Text oldTextValue = textValueObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        if (oldTextValue != null)
                        {
                            DestroyImmediate(oldTextValue);
                        }

                        if (textValueObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textValueObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = slider.currentValue.ToString("#0.00");
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = TextAlignmentOptions.Right;
                            t.color = slider.TextColor;
                        }
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        Transform textValueObjectTransform = vslider.transform.Find("Canvas/TextValue");
                        Text oldTextValue = textValueObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        TextAlignmentOptions align = TextAlignmentOptions.Right;
                        if (oldTextValue != null)
                        {
                            if (oldTextValue.alignment == TextAnchor.MiddleLeft)
                            {
                                align = TextAlignmentOptions.Left;
                            }
                            DestroyImmediate(oldTextValue);
                        }

                        if (textValueObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textValueObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = vslider.currentValue.ToString("#0.00");
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = align;
                            t.color = vslider.TextColor;
                        }
                    }

                    UISpinner spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {
                        bool hasText = (spinner.textAndValueVisibilityType == UISpinner.TextAndValueVisibilityType.ShowTextAndValue);

                        Transform textObjectTransform = spinner.transform.Find("Canvas/Text");
                        Text oldText = textObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        string oldTextContent = "";
                        if (oldText != null)
                        {
                            oldTextContent = oldText.text;
                            spinner.textContent = oldTextContent; // fix empty textContent.
                            DestroyImmediate(oldText);
                        }

                        if (textObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = spinner.textContent;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = TextAlignmentOptions.Left;
                            t.color = spinner.TextColor;

                            // hide if ValueOnly
                            textObjectTransform.gameObject.SetActive(hasText);
                        }

                        Transform textValueObjectTransform = spinner.transform.Find("Canvas/TextValue");
                        Text oldTextValue = textValueObjectTransform.gameObject.GetComponentInChildren<Text>(true);
                        if (oldTextValue != null)
                        {
                            DestroyImmediate(oldTextValue);
                        }

                        if (textValueObjectTransform.gameObject.GetComponent<TextMeshPro>() == null)
                        {
                            TextMeshPro t = textValueObjectTransform.gameObject.AddComponent<TextMeshPro>();
                            t.text = (spinner.spinnerValueType == UISpinner.SpinnerValueType.Float)
                                    ? spinner.FloatValue.ToString("#0.00")
                                    : spinner.IntValue.ToString();
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 1;
                            t.fontSizeMax = 500;
                            t.fontStyle = FontStyles.Normal;
                            t.alignment = hasText ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                            t.color = spinner.TextColor;
                        }
                    }

                    //UIPanel panel = element.GetComponent<UIPanel>();
                    //if (panel != null)
                    //{

                    //}

                    //UITimeBar timebar = element.GetComponent<UITimeBar>();
                    //if (timebar)
                    //{

                    //}

                    //UIColorPickerHue colorpickerhue = element.GetComponent<UIColorPickerHue>();
                    //if (colorpickerhue)
                    //{

                    //}

                    //UIColorPickerSaturation colorpickersat = element.GetComponent<UIColorPickerSaturation>();
                    //if (colorpickersat)
                    //{

                    //}

                    //UIColorPickerPreview colorpickerprev = element.GetComponent<UIColorPickerPreview>();
                    //if (colorpickerprev)
                    //{

                    //}

                    element.NeedsRebuild = true;
                }
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        //
        // FONT BOLD TO NORMAL
        //

        public void FONT_BoldToNormal()
        {
            int index = 0; // "0: Normal 1: Bold 2: Italic"
            for (int w = 0; w < windows.Length; ++w)
            {
                UIElement[] uiElements = windows[w].GetComponentsInChildren<UIElement>(true);
                for (int e = 0; e < uiElements.Length; ++e)
                {
                    UIElement element = uiElements[e];

                    UIButton button = element.GetComponent<UIButton>();
                    if (button != null)
                    {
                        Text t = button.gameObject.GetComponentInChildren<Text>(true);
                        if (t != null)
                        {
                            //t.fontStyle = FontStyle.Normal;
                            SerializedObject so = new SerializedObject(t);
                            so.FindProperty("m_FontData.m_FontStyle").enumValueIndex = index; // "Normal"
                            so.ApplyModifiedProperties();
                        }
                    }

                    UILabel label = element.GetComponent<UILabel>();
                    if (label != null)
                    {
                        Text t = label.gameObject.GetComponentInChildren<Text>(true);
                        if (t != null)
                        {
                            //t.fontStyle = FontStyle.Normal;
                            SerializedObject so = new SerializedObject(t);
                            so.FindProperty("m_FontData.m_FontStyle").enumValueIndex = index; // "Normal"
                            so.ApplyModifiedProperties();
                        }
                    }

                    UIPanel panel = element.GetComponent<UIPanel>();
                    if (panel != null)
                    {

                    }

                    UICheckbox checkbox = element.GetComponent<UICheckbox>();
                    if (checkbox != null)
                    {
                        Text t = checkbox.gameObject.GetComponentInChildren<Text>(true);
                        if (t != null)
                        {
                            //t.fontStyle = FontStyle.Normal;
                            SerializedObject so = new SerializedObject(t);
                            so.FindProperty("m_FontData.m_FontStyle").enumValueIndex = index; // "Normal"
                            so.ApplyModifiedProperties();
                        }
                    }

                    UISlider slider = element.GetComponent<UISlider>();
                    if (slider != null)
                    {
                        Text[] texts = slider.gameObject.GetComponentsInChildren<Text>();
                        foreach (Text t in texts)
                        {
                            //t.fontStyle = FontStyle.Normal;
                            SerializedObject so = new SerializedObject(t);
                            so.FindProperty("m_FontData.m_FontStyle").enumValueIndex = index; // "Normal"
                            so.ApplyModifiedProperties();
                        }
                    }

                    UIVerticalSlider vslider = element.GetComponent<UIVerticalSlider>();
                    if (vslider != null)
                    {
                        Text t = vslider.gameObject.GetComponentInChildren<Text>(true);
                        if (t != null)
                        {
                            //t.fontStyle = FontStyle.Normal;
                            SerializedObject so = new SerializedObject(t);
                            so.FindProperty("m_FontData.m_FontStyle").enumValueIndex = index; // "Normal"
                            so.ApplyModifiedProperties();
                        }
                    }

                    UISpinner spinner = element.GetComponent<UISpinner>();
                    if (spinner)
                    {
                        Text[] texts = spinner.gameObject.GetComponentsInChildren<Text>();
                        foreach (Text t in texts)
                        {
                            //t.fontStyle = FontStyle.Normal;
                            SerializedObject so = new SerializedObject(t);
                            so.FindProperty("m_FontData.m_FontStyle").enumValueIndex = index; // "Normal"
                            so.ApplyModifiedProperties();
                        }
                    }


                    UITimeBar timebar = element.GetComponent<UITimeBar>();
                    if (timebar)
                    {

                    }

                    UIColorPickerHue colorpickerhue = element.GetComponent<UIColorPickerHue>();
                    if (colorpickerhue)
                    {

                    }

                    UIColorPickerSaturation colorpickersat = element.GetComponent<UIColorPickerSaturation>();
                    if (colorpickersat)
                    {

                    }

                    UIColorPickerPreview colorpickerprev = element.GetComponent<UIColorPickerPreview>();
                    if (colorpickerprev)
                    {

                    }

                    element.NeedsRebuild = true;
                }
            }
        }

        //
        // Set Base Sprite
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

        public void UIOPTIONS_RandomChangeColors()
        {
            UIOptions.BackgroundColorVar.value = Random.ColorHSV();

            UIOPTIONS_Refresh();
        }

        //
        // Asset Bank Reorder
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


        #endregion

#endif
    }
}
