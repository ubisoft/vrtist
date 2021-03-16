/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using TMPro;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Builder for a camera.
    /// </summary>
    public class CameraBuilder : GameObjectBuilder
    {
        private const int RT_WIDTH = 1920 / 2;
        private const int RT_HEIGHT = 1080 / 2;
        private const int RT_DEPTH = 0;//24;

        public override GameObject CreateInstance(GameObject source, Transform parent = null, bool isPrefab = false)
        {
            GameObject newCamera = GameObject.Instantiate(source, parent);
            RenderTexture renderTexture = new RenderTexture(RT_WIDTH, RT_HEIGHT, RT_DEPTH, RenderTextureFormat.ARGB32);// RenderTextureFormat.Default);
            if (null == renderTexture)
                Debug.LogError("CAMERA FAILED");
            renderTexture.name = "Camera RT";

            newCamera.GetComponentInChildren<Camera>(true).targetTexture = renderTexture;
            newCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", renderTexture);

            VRInput.DeepSetLayer(newCamera, "CameraHidden");

            newCamera.GetComponentInChildren<CameraController>(true).CopyParameters(source.GetComponentInChildren<CameraController>(true));

            if (!GlobalState.Settings.DisplayGizmos)
                GlobalState.SetGizmoVisible(newCamera, false);

            // Add UI
            if (isPrefab)
            {
                Transform uiRoot = newCamera.transform.Find("Rotate/Name");

                if (null == uiRoot.Find("Name"))
                {
                    UILabel nameLabel = UILabel.Create(new UILabel.CreateLabelParams
                    {
                        parent = uiRoot,
                        widgetName = "Name",
                        caption = "Camera",
                        width = 1.4f,
                        height = 0.25f,
                        labelContent = UILabel.LabelContent.TextOnly,
                    });
                    {
                        TextMeshProUGUI text = nameLabel.gameObject.GetComponentInChildren<TextMeshProUGUI>();
                        text.enableAutoSizing = true;
                        text.fontSizeMin = 6f;
                        text.fontSizeMax = 72f;
                        text.alignment = TextAlignmentOptions.Center;
                    }
                    nameLabel.baseColor.useConstant = true;
                    nameLabel.baseColor.constant = new Color(0, 0, 0, 0);
                    nameLabel.SetLightLayer(2);
                }

                uiRoot = newCamera.transform.Find("Rotate/UI");
                if (null == uiRoot.Find("Focal"))
                {
                    // Focal slider
                    UISlider focalSlider = UISlider.Create(new UISlider.CreateArgs
                    {
                        parent = uiRoot,
                        widgetName = "Focal",
                        caption = "Focal",
                        currentValue = 35f,
                        sliderBegin = 0.15f,
                        sliderEnd = 0.86f,
                        relativeLocation = new Vector3(-0.30f, -0.03f, -UISlider.default_thickness),
                        width = 0.3f,
                        height = 0.02f,
                        railMargin = 0.002f,
                        knobRadius = 0.007f
                    });
                    focalSlider.DataCurve = GlobalState.Settings.focalCurve;

                    focalSlider.SetLightLayer(2);
                }

                // In front button
                if (null == uiRoot.Find("InFront"))
                {
                    UIButton inFrontButton = UIButton.Create(new UIButton.CreateButtonParams
                    {
                        parent = uiRoot,
                        widgetName = "InFront",
                        caption = "Always in Front",
                        buttonContent = UIButton.ButtonContent.ImageOnly,
                        icon = UIUtils.LoadIcon("back"),
                        width = 0.02f,
                        height = 0.02f,
                        iconMarginBehavior = UIButton.IconMarginBehavior.UseIconMargin,
                        iconMargin = 0.002f,
                        relativeLocation = new Vector3(-0.30f, -0.005f, -UIButton.default_thickness)
                    });
                    inFrontButton.isCheckable = true;
                    inFrontButton.baseSprite = UIUtils.LoadIcon("back");
                    inFrontButton.checkedSprite = UIUtils.LoadIcon("front");
                    inFrontButton.SetLightLayer(2);
                }

                // Touch screen
                uiRoot = newCamera.transform.Find("Rotate/CameraPreview");
                UITouchScreen touchScreen = UITouchScreen.Create(new UITouchScreen.CreateTouchScreenParams
                {
                    parent = uiRoot,
                    width = 1f,
                    height = 1f,
                    thickness = 0.01f,
                    relativeLocation = new Vector3(-0.5f, 0.5f, -0.03f)
                });
                touchScreen.SetLightLayer(2);
                touchScreen.gameObject.SetActive(false);

                // Focus button
                uiRoot = newCamera.transform.Find("Rotate/UI");
                UIButton focusButton = UIButton.Create(new UIButton.CreateButtonParams
                {
                    parent = uiRoot,
                    widgetName = "FocusButton",
                    caption = "Enable Touch Screen Focus",
                    buttonContent = UIButton.ButtonContent.ImageOnly,
                    icon = UIUtils.LoadIcon("dof"),
                    width = 0.02f,
                    height = 0.02f,
                    iconMarginBehavior = UIButton.IconMarginBehavior.UseIconMargin,
                    iconMargin = 0.002f,
                    relativeLocation = new Vector3(-0.27f, -0.005f, -UIButton.default_thickness)
                });
                focusButton.isCheckable = true;
                focusButton.baseSprite = UIUtils.LoadIcon("dof");
                focusButton.checkedSprite = UIUtils.LoadIcon("dof");
                focusButton.SetLightLayer(2);
            }

            return newCamera;
        }
    }
}
