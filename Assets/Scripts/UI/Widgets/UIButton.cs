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
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIButton : UIElement
    {
        public enum IconMarginBehavior { UseButtonMargin, UseIconMargin };
        public enum ButtonContent { TextOnly, ImageOnly, TextAndImage };

        // TODO: put in a scriptable object
        public static readonly string default_widget_name = "New Button";
        public static readonly float default_width = 0.15f;
        public static readonly float default_height = 0.05f;
        public static readonly float default_margin = 0.005f;
        public static readonly float default_thickness = 0.001f;
        public static readonly string default_material_name = "UIBase";
        public static readonly string default_text = "Button";
        public static readonly string default_icon_name = "paint";
        public static readonly ButtonContent default_content = ButtonContent.TextAndImage;
        public static readonly IconMarginBehavior default_icon_margin_behavior = IconMarginBehavior.UseButtonMargin;
        public static readonly float default_icon_margin = 0.0f;

        [SpaceHeader("Button Shape Parameters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float margin = default_margin;
        [CentimeterFloat] public float thickness = default_thickness;
        public IconMarginBehavior iconMarginBehavior = default_icon_margin_behavior;
        [CentimeterFloat] public float iconMargin = default_icon_margin;
        public ButtonContent content = default_content;
        public Sprite baseSprite = null;
        public Sprite checkedSprite = null;
        public ColorReference checkedColor = new ColorReference();
        [TextArea] public string textContent = "";
        public Material source_material = null;

        [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
        public int nbSubdivCornerFixed = 3;
        public int nbSubdivCornerPerUnit = 3;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public UnityEvent onClickEvent = new UnityEvent();
        public UnityEvent onReleaseEvent = new UnityEvent();
        public bool isCheckable = false;
        public BoolChangedEvent onCheckEvent = new BoolChangedEvent();
        public UnityEvent onHoverEvent = new UnityEvent();

        public string Text { get { return textContent; } set { SetText(value); } }
        public Color CheckedColor { get { return checkedColor.Value; } }

        public Color ImageColor
        {
            get
            {
                Image image = transform.GetComponentInChildren<Image>(true);
                return image.color;
            }
            set
            {
                Image image = transform.GetComponentInChildren<Image>(true);
                image.color = value;
                imageColorFixed = true;
            }
        }
        private bool imageColorFixed = false;

        private bool isChecked = false;
        public bool Checked
        {
            get { return isChecked; }
            set { isChecked = value; ResetColor(); UpdateCheckIcon(); }
        }

        private bool grippedUnderThreshold = false;
        private Vector2 grippedPos = Vector2.zero;

        public override void ResetColor()
        {
            SetColor(Disabled ? DisabledColor
                  : (Pushed ? PushedColor
                  : (Checked ? CheckedColor
                  : (Selected ? SelectedColor
                  : (Hovered ? HoveredColor
                  : BaseColor)))));

            SetForegroundColor(Disabled ? DisabledTextColor : TextColor);

            // Make the canvas pop front if Hovered.
            Canvas c = GetComponentInChildren<Canvas>();
            if (c != null)
            {
                RectTransform rt = c.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localPosition = Hovered ? new Vector3(0, 0, -0.003f) : Vector3.zero;
                }
            }
        }

        public override void SetForegroundColor(Color color)
        {
            Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                // IMAGE
                if (!imageColorFixed)
                {
                    Image image = canvas.GetComponentInChildren<Image>();
                    if (image != null)
                    {
                        image.color = color;
                    }
                }

                // TEXT
                TextMeshProUGUI text = canvas.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.color = color;
                }
            }
        }

        public override void RebuildMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBoxEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            theNewMesh.name = "UIButton_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateCanvasDimensions();
        }

        private void UpdateColliderDimensions()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            if (meshFilter != null && coll != null)
            {
                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                {
                    coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                    coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                }
                else
                {
                    coll.center = initColliderCenter;
                    coll.size = initColliderSize;
                }
            }
        }

        private void UpdateCheckIcon()
        {
            Image img = gameObject.GetComponentInChildren<Image>();
            if (img != null && gameObject.activeSelf && isCheckable)
            {
                img.sprite = isChecked ? checkedSprite : baseSprite;
                img.enabled = (null != img.sprite);
            }
        }

        private void UpdateCanvasDimensions()
        {
            Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRT = canvas.gameObject.GetComponent<RectTransform>();
                canvasRT.sizeDelta = new Vector2(width, height);

                float minSide = Mathf.Min(width, height);

                // IMAGE
                Image image = canvas.GetComponentInChildren<Image>(true);
                if (image != null)
                {
                    if (content != ButtonContent.TextOnly)
                    {
                        image.gameObject.SetActive(true);

                        RectTransform rt = image.gameObject.GetComponent<RectTransform>();
                        if (rt)
                        {
                            float m = iconMarginBehavior == IconMarginBehavior.UseButtonMargin ? margin : iconMargin;
                            float offsetx = content == ButtonContent.TextAndImage ? 0.0f : (width - minSide) / 2.0f;
                            float offsety = content == ButtonContent.TextAndImage ? 0.0f : (height - minSide) / 2.0f;
                            rt.sizeDelta = new Vector2(minSide - 2.0f * m, minSide - 2.0f * m);
                            rt.localPosition = new Vector3(m + offsetx, -m - offsety, -0.001f);
                        }
                    }
                    else
                    {
                        image.gameObject.SetActive(false);
                    }
                }

                // TEXT
                TextMeshProUGUI text = canvas.gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null)
                {
                    if (content != ButtonContent.ImageOnly)
                    {
                        text.gameObject.SetActive(true);

                        text.text = Text;
                        text.color = TextColor;

                        RectTransform rt = text.gameObject.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            if (content == ButtonContent.TextAndImage)
                            {
                                rt.sizeDelta = new Vector2((width - minSide - margin) * 100.0f, (height - 2.0f * margin) * 100.0f);
                                rt.localPosition = new Vector3(minSide, -margin, -0.002f);
                            }
                            else // TextOnly
                            {
                                rt.sizeDelta = new Vector2((width - 2.0f * margin) * 100.0f, (height - 2.0f * margin) * 100.0f);
                                rt.localPosition = new Vector3(margin, -margin, -0.002f);
                            }
                        }
                    }
                    else
                    {
                        text.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_thickness = 0.001f;
            const int min_nbSubdivCornerFixed = 1;
            const int min_nbSubdivCornerPerUnit = 1;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (thickness < min_thickness)
                thickness = min_thickness;
            if (margin > width / 2.0f || margin > height / 2.0f)
                margin = Mathf.Min(width / 2.0f, height / 2.0f);
            if (nbSubdivCornerFixed < min_nbSubdivCornerFixed)
                nbSubdivCornerFixed = min_nbSubdivCornerFixed;
            if (nbSubdivCornerPerUnit < min_nbSubdivCornerPerUnit)
                nbSubdivCornerPerUnit = min_nbSubdivCornerPerUnit;

            // Realign button to parent anchor if we change the thickness.
            if (-thickness != relativeLocation.z)
                relativeLocation.z = -thickness;

            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                ResetColor();
                NeedsRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(0, 0, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width, 0, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(0, -height, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width, -height, -0.001f));
            Vector3 posTopLeftM = transform.TransformPoint(new Vector3(margin, -margin, -0.001f));
            Vector3 posTopRightM = transform.TransformPoint(new Vector3(width - margin, -margin, -0.001f));
            Vector3 posBottomLeftM = transform.TransformPoint(new Vector3(margin, -height + margin, -0.001f));
            Vector3 posBottomRightM = transform.TransformPoint(new Vector3(width - margin, -height + margin, -0.001f));

            Gizmos.color = new Color(.6f, .6f, .6f);
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

            Gizmos.color = new Color(.5f, .5f, .5f);
            Gizmos.DrawLine(posTopLeftM, posTopRightM);
            Gizmos.DrawLine(posTopRightM, posBottomRightM);
            Gizmos.DrawLine(posBottomRightM, posBottomLeftM);
            Gizmos.DrawLine(posBottomLeftM, posTopLeftM);

            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        public override void ResetMaterial()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = BaseColor;

                Material materialInstance = Instantiate(source_material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIButton_Material_Instance";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        private void SetText(string textValue)
        {
            textContent = textValue;

            TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = textValue;
            }
        }

        #region ray

        public override void OnRayEnter()
        {
            base.OnRayEnter();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayEnterClicked()
        {
            base.OnRayEnterClicked();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayHover(Ray ray)
        {
            base.OnRayHover(ray);
            onHoverEvent.Invoke();
        }

        public override void OnRayHoverClicked()
        {
            base.OnRayHoverClicked();
            onHoverEvent.Invoke();
        }

        public override void OnRayExit()
        {
            base.OnRayExit();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayExitClicked()
        {
            base.OnRayExitClicked();
            WidgetBorderHapticFeedback();
        }

        public override void OnRayClick()
        {
            base.OnRayClick();
            onClickEvent.Invoke();
        }

        public override void OnRayReleaseInside()
        {
            onReleaseEvent.Invoke();

            Hovered = true;
            Pushed = false;

            if (isCheckable)
            {
                Checked = !Checked;
                onCheckEvent.Invoke(Checked);
            }

            ResetColor();

            grippedUnderThreshold = false;
        }

        public override bool OnRayReleaseOutside()
        {
            //return base.OnRayReleaseOutside();

            bool validate = false;

            if (grippedUnderThreshold)
            {
                validate = true;
                onReleaseEvent.Invoke();
                if (isCheckable)
                {
                    Checked = !Checked;
                    onCheckEvent.Invoke(Checked);
                }
            }

            Hovered = false;
            Pushed = false;
            ResetColor();

            grippedUnderThreshold = false;

            return validate;
        }

        public override bool OverridesRayEndPoint() { return true; }

        public override void OverrideRayEndPoint(Ray ray, ref Vector3 rayEndPoint)
        {
            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

            // Project ray on the widget plane.
            Plane widgetPlane = new Plane(-transform.forward, transform.position);
            widgetPlane.Raycast(ray, out float enter);
            Vector3 worldCollisionOnWidgetPlane = ray.GetPoint(enter);

            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCollisionOnWidgetPlane);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            if (IgnoreRayInteraction())
            {
                rayEndPoint = transform.TransformPoint(localProjectedWidgetPosition);
                return;
            }


            //Vector2 localCenter = new Vector2(width / 2.0f, -height / 2.0f);
            Vector2 project = new Vector3(localWidgetPosition.x, localWidgetPosition.y);
            if (triggerJustClicked)
            {
                grippedPos = project;
            }

            if (Vector2.Distance(project, grippedPos) < 3.0f * height)
            {
                grippedUnderThreshold = true;
                localProjectedWidgetPosition.x = grippedPos.x;
                localProjectedWidgetPosition.y = grippedPos.y;
            }
            else
            {
                grippedUnderThreshold = false;
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            rayEndPoint = worldProjectedWidgetPosition;
        }

        #endregion

        #region create

        public class CreateButtonParams
        {
            public Transform parent = null;
            public string widgetName = UIButton.default_widget_name;
            public Vector3 relativeLocation = new Vector3(0, 0, -UIButton.default_thickness);
            public float width = UIButton.default_width;
            public float height = UIButton.default_height;
            public float margin = UIButton.default_margin;
            public float thickness = UIButton.default_thickness;
            public Material material = UIUtils.LoadMaterial(UIButton.default_material_name);
            public ColorVar bgcolor = UIOptions.BackgroundColorVar;
            public ColorVar fgcolor = UIOptions.ForegroundColorVar;
            public ColorVar pushedColor = UIOptions.PushedColorVar;
            public ColorVar selectedColor = UIOptions.SelectedColorVar;
            public ColorVar checkedColor = UIOptions.CheckedColorVar;
            public ButtonContent buttonContent = UIButton.default_content;
            public IconMarginBehavior iconMarginBehavior = UIButton.default_icon_margin_behavior;
            public float iconMargin = UIButton.default_icon_margin;
            public string caption = UIButton.default_text;
            public Sprite icon = UIUtils.LoadIcon(UIButton.default_icon_name);
        }

        public static UIButton Create(CreateButtonParams input)
        {
            GameObject go = new GameObject(input.widgetName)
            {
                tag = "UICollider"
            };

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (input.parent)
            {
                UIElement elem = input.parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIButton uiButton = go.AddComponent<UIButton>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiButton.relativeLocation = input.relativeLocation;
            uiButton.transform.parent = input.parent;
            uiButton.transform.localPosition = parentAnchor + input.relativeLocation;
            uiButton.transform.localRotation = Quaternion.identity;
            uiButton.transform.localScale = Vector3.one;
            uiButton.width = input.width;
            uiButton.height = input.height;
            uiButton.margin = input.margin;
            uiButton.thickness = input.thickness;
            uiButton.content = input.buttonContent;
            uiButton.textContent = input.caption;
            uiButton.baseSprite = input.icon;
            uiButton.iconMarginBehavior = input.iconMarginBehavior;
            uiButton.iconMargin = input.iconMargin;
            uiButton.source_material = input.material;
            uiButton.baseColor.useConstant = false;
            uiButton.baseColor.reference = input.bgcolor;
            uiButton.textColor.useConstant = false;
            uiButton.textColor.reference = input.fgcolor;
            uiButton.pushedColor.useConstant = false;
            uiButton.pushedColor.reference = input.pushedColor;
            uiButton.selectedColor.useConstant = false;
            uiButton.selectedColor.reference = input.selectedColor;
            uiButton.checkedColor.useConstant = false;
            uiButton.checkedColor.reference = input.checkedColor;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(input.width, input.height, input.margin, input.thickness);
                uiButton.Anchor = Vector3.zero;
                BoxCollider coll = go.GetComponent<BoxCollider>();
                if (coll != null)
                {
                    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                    if (initColliderSize.z < UIElement.collider_min_depth_shallow)
                    {
                        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_shallow / 2.0f);
                        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_shallow);
                    }
                    else
                    {
                        coll.center = initColliderCenter;
                        coll.size = initColliderSize;
                    }
                    coll.isTrigger = true;
                }
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && uiButton.source_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(uiButton.source_material);
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"

                uiButton.SetColor(input.bgcolor.value);
            }

            //
            // CANVAS
            //
            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = uiButton.transform;

            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvas.GetComponent<RectTransform>(); // auto added when adding Canvas
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); // top left
            rt.sizeDelta = new Vector2(uiButton.width, uiButton.height);
            rt.localPosition = Vector3.zero;

            CanvasScaler cs = canvas.AddComponent<CanvasScaler>();
            cs.dynamicPixelsPerUnit = 300; // 300 dpi, sharp font
            cs.referencePixelsPerUnit = 100; // default?

            float minSide = Mathf.Min(uiButton.width, uiButton.height);

            //
            // IMAGE
            //
            GameObject image = new GameObject("Image");
            image.transform.parent = canvas.transform;

            Image img = image.AddComponent<Image>();
            img.sprite = input.icon;
            img.color = input.fgcolor.value;

            RectTransform irt = image.GetComponent<RectTransform>();
            irt.localScale = Vector3.one;
            irt.localRotation = Quaternion.identity;
            irt.anchorMin = new Vector2(0, 1);
            irt.anchorMax = new Vector2(0, 1);
            irt.pivot = new Vector2(0, 1); // top left
            // TODO: non square icons ratio...
            if (uiButton.iconMarginBehavior == IconMarginBehavior.UseButtonMargin)
            {
                irt.sizeDelta = new Vector2(minSide - 2.0f * input.margin, minSide - 2.0f * input.margin);
                irt.localPosition = new Vector3(input.margin, -input.margin, -0.001f);
            }
            else // IconMarginBehavior.UseIconMargin for the moment
            {
                irt.sizeDelta = new Vector2(minSide - 2.0f * uiButton.iconMargin, minSide - 2.0f * uiButton.iconMargin);
                irt.localPosition = new Vector3(uiButton.iconMargin, -uiButton.iconMargin, -0.001f);
            }

            image.SetActive(input.buttonContent != ButtonContent.TextOnly);

            //
            // TEXT
            //
            GameObject text = new GameObject("Text");
            text.transform.parent = canvas.transform;

            TextMeshProUGUI t = text.AddComponent<TextMeshProUGUI>();
            t.text = input.caption;
            t.enableAutoSizing = false;
            t.fontSize = 16;
            t.fontSizeMin = 18;
            t.fontSizeMax = 18;
            t.fontStyle = FontStyles.Normal;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.color = input.fgcolor.value;

            RectTransform trt = t.GetComponent<RectTransform>();
            trt.localScale = 0.01f * Vector3.one;
            trt.localRotation = Quaternion.identity;
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, 1); // top left

            // TODO: option for V Margin.

            if (input.buttonContent == ButtonContent.TextAndImage)
            {
                trt.sizeDelta = new Vector2((input.width - minSide - input.margin) * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                trt.localPosition = new Vector3(minSide, 0.0f, -0.002f);
            }
            else // TextOnly
            {
                trt.sizeDelta = new Vector2((input.width - 2.0f * input.margin) * 100.0f, (input.height - 2.0f * input.margin) * 100.0f);
                trt.localPosition = new Vector3(input.margin, -input.margin, -0.002f);
            }

            text.SetActive(input.buttonContent != ButtonContent.ImageOnly);

            UIUtils.SetRecursiveLayer(go, "CameraHidden");

            return uiButton;
        }
        #endregion
    }
}
