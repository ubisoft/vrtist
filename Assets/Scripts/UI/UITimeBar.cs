using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UITimeBar : UIElement
    {
        private static readonly string default_widget_name = "TimeBar";
        private static readonly float default_width = 0.3f;
        private static readonly float default_height = 0.03f;
        private static readonly float default_thickness = 0.001f;
        private static readonly int default_min_value = 0;
        private static readonly int default_max_value = 250;
        private static readonly int default_current_value = 0;
        private static readonly string default_background_material_name = "UIBase";
        private static readonly Color default_color = UIElement.default_background_color;
        private static readonly string default_text = "TimeBar";

        [SpaceHeader("TimeBar Base Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        [CentimeterFloat] public float thickness = 0.001f;
        public Color pushedColor = new Color(0.3f, 0.3f, 0.3f);

        [SpaceHeader("TimeBar Values", 6, 0.8f, 0.8f, 0.8f)]
        public int minValue = 0;
        public int maxValue = 250;
        public int currentValue = 0;
        
        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public IntChangedEvent onSlideEvent = new IntChangedEvent();
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        [SerializeField] private Transform knob = null;

        private bool needRebuild = false;

        public int MinValue { get { return GetMinValue(); } set { SetMinValue(value); UpdateTimeBarPosition(); } }
        public int MaxValue { get { return GetMaxValue(); } set { SetMaxValue(value); UpdateTimeBarPosition(); } }
        public int Value { get { return GetValue(); } set { SetValue(value); UpdateTimeBarPosition(); } }

        void Start()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                onSlideEvent.AddListener(OnSlide);
                onClickEvent.AddListener(OnClickTimeBar);
                onReleaseEvent.AddListener(OnReleaseTimeBar);
            }
        }

        public override void RebuildMesh()
        {
            // TIME TICKS ??
            // ...

            // BASE
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(width, height, thickness);
            theNewMesh.name = "UITimeBar_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
            UpdateTimeBarPosition();
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

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_thickness = 0.001f;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (thickness < min_thickness)
                thickness = min_thickness;
            if (currentValue < minValue)
                currentValue = minValue;
            if (currentValue > maxValue)
                currentValue = maxValue;

            needRebuild = true;
        }

        private void Update()
        {
            // NOTE: rebuild when touching a property in the inspector.
            // Boolean needRebuild is set in OnValidate();
            // The rebuild method called when using the gizmos is: Width and Height
            // properties in UIElement.
            // This comment is probably already obsolete.
            if (needRebuild)
            {
                // NOTE: I do all these things because properties can't be called from the inspector.
                try
                {
                    RebuildMesh();
                    UpdateLocalPosition();
                    UpdateAnchor();
                    UpdateChildren();
                    UpdateTimeBarPosition();
                    SetColor(baseColor.Value);
                }
                catch(Exception e)
                {
                    Debug.Log("Exception: " + e);
                }

                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -height / 2.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(0.0f, 0.0f, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width, 0.0f, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(0.0f, -height, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width, -height, -0.001f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);

#if UNITY_EDITOR
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
        }

        public override void ResetMaterial()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Color prevColor = BaseColor;
                if (meshRenderer.sharedMaterial != null)
                {
                    prevColor = meshRenderer.sharedMaterial.GetColor("_BaseColor");
                }

                Material material = UIUtils.LoadMaterial("UIPanel");
                Material materialInstance = Instantiate(material);

                meshRenderer.sharedMaterial = materialInstance;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                Material sharedMaterialInstance = meshRenderer.sharedMaterial;
                sharedMaterialInstance.name = "UIPanel_Instance_for_UITimebar";
                sharedMaterialInstance.SetColor("_BaseColor", prevColor);
            }
        }

        private void UpdateTimeBarPosition()
        {
            float pct = (float)(currentValue - minValue) / (float)(maxValue - minValue);

            float startX = 0.0f;
            float endX = width;
            float posX = startX + pct * (endX - startX);

            Vector3 knobPosition = new Vector3(posX, 0.0f, 0.0f);

            knob.localPosition = knobPosition;
        }

        private int GetMinValue()
        {
            return minValue;
        }

        private void SetMinValue(int intValue)
        {
            minValue = intValue;
        }

        private int GetMaxValue()
        {
            return maxValue;
        }

        private void SetMaxValue(int intValue)
        {
            maxValue = intValue;
        }

        private int GetValue()
        {
            return currentValue;
        }

        private void SetValue(int intValue)
        {
            currentValue = intValue;
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                // HIDE cursor

                onClickEvent.Invoke();
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                // SHOW cursor

                onReleaseEvent.Invoke();
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionStay())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                // NOTE: The correct "currentValue" is already computed in the HandleCursorBehavior callback.
                //       Just call the listeners here.
                onSlideEvent.Invoke(currentValue);
            }
        }

        public void OnClickTimeBar()
        {
            SetColor(pushedColor);
        }

        public void OnReleaseTimeBar()
        {
            SetColor(baseColor.Value);
        }

        public void OnSlide(int f)
        {
            //Value = f; // Value already set in HandleCursorBehavior
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCursorColliderCenter);
            Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            float startX = 0.0f;
            float endX = width;
            float snapXDistance = 0.002f; // for snapping of a little bit to the right/left of extremities
            if (localProjectedWidgetPosition.x > startX - snapXDistance && localProjectedWidgetPosition.x < endX + snapXDistance)
            {
                // SNAP X left
                if (localProjectedWidgetPosition.x < startX)
                    localProjectedWidgetPosition.x = startX;

                // SNAP X right
                if(localProjectedWidgetPosition.x > endX)
                    localProjectedWidgetPosition.x = endX;

                // Compute closest int for snapping.
                float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);
                float fValue = (float)minValue + pct * (float)(maxValue - minValue);
                int roundedValue = Mathf.RoundToInt(fValue);
                Value = roundedValue; // will replace the slider knob.

                // SNAP X to closest int
                localProjectedWidgetPosition.x = startX + ((float)roundedValue - minValue) * (endX - startX) / (float)(maxValue - minValue);
                // SNAP Y to middle of knob object. TODO: use actual knob dimensions
                localProjectedWidgetPosition.y = -height + 0.02f;
                // SNAP Z to the thickness of the knob
                localProjectedWidgetPosition.z = -0.005f;

                // Haptic intensity as we go deeper into the widget.
                float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                intensity *= intensity; // ease-in

                // TODO : Re-enable
                VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
            }

            Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            cursorShapeTransform.position = worldProjectedWidgetPosition;
        }

        //
        // CREATE
        //

        public class CreateArgs
        {
            public Transform parent = null;
            public string widgetName = UITimeBar.default_widget_name;
            public Vector3 relativeLocation;
            public float width;
            public float height;
            public float thickness = UITimeBar.default_thickness;
            public int min_slider_value = UITimeBar.default_min_value;
            public int max_slider_value = UITimeBar.default_max_value;
            public int cur_slider_value = UITimeBar.default_current_value;
            public Material background_material = UIUtils.LoadMaterial(UITimeBar.default_background_material_name);
            public ColorVariable background_color = UIOptions.Instance.backgroundColor;
            public string caption = UITimeBar.default_text;
        }

        public static void Create(CreateArgs input)
        {
            GameObject go = new GameObject(input.widgetName);
            go.tag = "UICollider";

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

            UITimeBar uiTimeBar = go.AddComponent<UITimeBar>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiTimeBar.relativeLocation = input.relativeLocation;
            uiTimeBar.transform.parent = input.parent;
            uiTimeBar.transform.localPosition = parentAnchor + input.relativeLocation;
            uiTimeBar.transform.localRotation = Quaternion.identity;
            uiTimeBar.transform.localScale = Vector3.one;
            uiTimeBar.width = input.width;
            uiTimeBar.height = input.height;
            uiTimeBar.thickness = input.thickness;
            uiTimeBar.minValue = input.min_slider_value;
            uiTimeBar.maxValue = input.max_slider_value;
            uiTimeBar.currentValue = input.cur_slider_value;
            uiTimeBar.baseColor.useConstant = false;
            uiTimeBar.baseColor.reference = input.background_color;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // TODO: new mesh, with time ticks texture
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(input.width, input.height, input.thickness);
                uiTimeBar.Anchor = Vector3.zero;
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
            if (meshRenderer != null && input.background_material != null)
            {
                // Clone the material.
                meshRenderer.sharedMaterial = Instantiate(input.background_material);

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 4; // "LightLayer 3"

                uiTimeBar.SetColor(input.background_color.value);
            }

            // KNOB
            GameObject K = new GameObject("Knob");
            uiTimeBar.knob = K.transform;
        }
    }
}
