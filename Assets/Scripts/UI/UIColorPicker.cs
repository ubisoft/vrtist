using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

namespace VRtist
{
    public partial class UIColorPickerSaturation { }
    public partial class UIColorPickerHue { }
    public partial class UIColorPickerPreview { }

    [ExecuteInEditMode]
    public class UIColorPicker : UIElement
    {
        [SpaceHeader("Button Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
        public float thickness = 0.001f;
        public float padding = 0.01f;
        
        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public ColorChangedEvent onColorChangedEvent = new ColorChangedEvent();

        private UIColorPickerSaturation saturation = null;
        private UIColorPickerHue hue = null;
        private UIColorPickerPreview preview = null;

        private bool needRebuild = false;

        void Start()
        {
        }

        public override void RebuildMesh()
        {
            saturation.RebuildMesh();
            hue.RebuildMesh();
            preview.RebuildMesh();
        }

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_thickness = 0.001f;
            const float min_padding = 0.0f;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (thickness < min_thickness)
                thickness = min_thickness;
            if (padding < min_padding)
                padding = min_padding;
            
            // TODO: Test max padding relative to global width.
            //       See UIButton or UIPanel for examples about margin vs width

            needRebuild = true;
        }

        private void Update()
        {
            if (needRebuild)
            {
                // NOTE: I do all these things because properties can't be called from the inspector.
                RebuildMesh();
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();
                needRebuild = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 labelPosition = transform.TransformPoint(new Vector3(width / 4.0f, -2.0f * height / 3.0f, -0.001f));
            Vector3 posTopLeft = transform.TransformPoint(new Vector3(0,0, -0.001f));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width, 0, -0.001f));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(0, -height, -0.001f));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width, -height, -0.001f));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
        }

        //private void OnTriggerEnter(Collider otherCollider)
        //{
        //    // TODO: pass the Cursor to the button, test the object instead of a hardcoded name.
        //    if (otherCollider.gameObject.name == "Cursor")
        //    {
        //        onClickEvent.Invoke();
        //        VRInput.SendHaptic(VRInput.rightController, 0.03f);
        //    }
        //}

        //private void OnTriggerExit(Collider otherCollider)
        //{
        //    if (otherCollider.gameObject.name == "Cursor")
        //    {
        //        onReleaseEvent.Invoke();
        //    }
        //}

        //private void OnTriggerStay(Collider otherCollider)
        //{
        //    if (otherCollider.gameObject.name == "Cursor")
        //    {
        //        onHoverEvent.Invoke();
        //    }
        //}

        public static void CreateUIColorPicker(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            float padding,
            Material saturationMaterial,
            Material hueMaterial,
            Material previewMaterial)
        {
            GameObject go = new GameObject(objectName);

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIColorPicker uiColorPicker = go.AddComponent<UIColorPicker>();
            uiColorPicker.relativeLocation = relativeLocation;
            uiColorPicker.transform.parent = parent;
            uiColorPicker.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPicker.transform.localRotation = Quaternion.identity;
            uiColorPicker.transform.localScale = Vector3.one;
            uiColorPicker.width = width;
            uiColorPicker.height = height;
            uiColorPicker.thickness = thickness;
            uiColorPicker.padding = padding;

            // TODO: create saturation, hue and preview
            // 1) find the 3 children positions and dimensions
            // 2) create them, give them "this" as a parent, make it return the component we need for step 3
            // 3) set saturation/hue/preview variables with the returned component.

            uiColorPicker.saturation = UIColorPickerSaturation.CreateUIColorPickerSaturation("saturation", go.transform);
            uiColorPicker.hue = UIColorPickerHue.CreateUIColorPickerHue("hue", go.transform);
            uiColorPicker.preview = UIColorPickerPreview.CreateUIColorPickerPreview("preview", go.transform);

            //// Setup the Meshfilter
            //MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            //if (meshFilter != null)
            //{
            //    meshFilter.sharedMesh = UIButton.BuildRoundedRect(width, height, margin);
            //    uiButton.Anchor = Vector3.zero;
            //    BoxCollider coll = go.GetComponent<BoxCollider>();
            //    if (coll != null)
            //    {
            //        Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
            //        Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
            //        if (initColliderSize.z < UIElement.collider_depth)
            //        {
            //            coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_depth / 2.0f);
            //            coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_depth);
            //        }
            //        else
            //        {
            //            coll.center = initColliderCenter;
            //            coll.size = initColliderSize;
            //        }
            //        coll.isTrigger = true;
            //    }
            //}

            //// Setup the MeshRenderer
            //MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            //if (meshRenderer != null && material != null)
            //{
            //    // TODO: see if we need to Instantiate(uiMaterial), or modify the instance created when calling meshRenderer.material
            //    //       to make the error disappear;

            //    // Get an instance of the same material
            //    // NOTE: sends an warning about leaking instances, because meshRenderer.material create instances while we are in EditorMode.
            //    //meshRenderer.sharedMaterial = uiMaterial;
            //    //Material material = meshRenderer.material; // instance of the sharedMaterial

            //    // Clone the material.
            //    meshRenderer.sharedMaterial = Instantiate(material);
            //    Material sharedMaterial = meshRenderer.sharedMaterial;

            //    uiButton.BaseColor = color;
            //}
        }
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public partial class UIColorPickerSaturation : MonoBehaviour
    {
        public void RebuildMesh()
        {
            //MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            //Mesh theNewMesh = UIButton.BuildRoundedRectEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
            //theNewMesh.name = "UIButton_GeneratedMesh";
            //meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
        }

        public void UpdateColliderDimensions()
        {
            //    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            //    BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            //    if (meshFilter != null && coll != null)
            //    {
            //        Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
            //        Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
            //        if (initColliderSize.z < UIElement.collider_depth)
            //        {
            //            coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_depth / 2.0f);
            //            coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_depth);
            //        }
            //        else
            //        {
            //            coll.center = initColliderCenter;
            //            coll.size = initColliderSize;
            //        }
            //    }
        }

        public static UIColorPickerSaturation CreateUIColorPickerSaturation(
            string objectName,
            Transform parent)
        {
            GameObject go = new GameObject(objectName);

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIColorPickerSaturation uiColorPickerSaturation = go.AddComponent<UIColorPickerSaturation>();



            // TODO



            return uiColorPickerSaturation;
        }
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public partial class UIColorPickerHue : MonoBehaviour
    {
        public void RebuildMesh()
        {
            UpdateColliderDimensions();
        }

        public void UpdateColliderDimensions()
        {
        }

        public static UIColorPickerHue CreateUIColorPickerHue(
            string objectName,
            Transform parent)
        {
            GameObject go = new GameObject(objectName);

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIColorPickerHue uiColorPickerHue = go.AddComponent<UIColorPickerHue>();



            // TODO



            return uiColorPickerHue;
        }
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
        RequireComponent(typeof(MeshRenderer)),
        RequireComponent(typeof(BoxCollider))]
    public partial class UIColorPickerPreview : MonoBehaviour
    {
        public void RebuildMesh()
        {
            UpdateColliderDimensions();
        }

        public void UpdateColliderDimensions()
        {
        }

        public static UIColorPickerPreview CreateUIColorPickerPreview(
            string objectName,
            Transform parent)
        {
            GameObject go = new GameObject(objectName);

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIColorPickerPreview uiColorPickerPreview = go.AddComponent<UIColorPickerPreview>();



            // TODO



            return uiColorPickerPreview;
        }
    }
}
