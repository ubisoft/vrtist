using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
     //RequireComponent(typeof(BoxCollider))]
    public class UISliderRail : MonoBehaviour
    {
        public float width;
        public float height;
        public float thickness;
        public float margin;

        // TODO: OnValidate() on width/heigt/etc... ??? Controlled by UISlider already.

        private Color _color;
        public Color Color { get { return _color; } set { _color = value; ApplyColor(_color); } }

        void Awake()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                // Why did I copy these lines from the ColorPicker???

                width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
                //margin = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness, float newMargin)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildHollowCubeEx(width, height, margin, thickness);
            theNewMesh.name = "UISliderRail_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;
            margin = newMargin;

            //UpdateColliderDimensions();
        }

        //public void UpdateColliderDimensions()
        //{
        //    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        //    BoxCollider coll = gameObject.GetComponent<BoxCollider>();
        //    if (meshFilter != null && coll != null)
        //    {
        //        Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
        //        Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
        //        if (initColliderSize.z < UIElement.collider_min_depth_deep)
        //        {
        //            coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_deep / 2.0f);
        //            coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_deep);
        //        }
        //        else
        //        {
        //            coll.center = initColliderCenter;
        //            coll.size = initColliderSize;
        //        }
        //    }
        //}

        private void ApplyColor(Color c)
        {
            GetComponent<MeshRenderer>().material.SetColor("_BaseColor", c);
        }

        public static UISliderRail CreateUISliderRail(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            float margin,
            Material material,
            Color c)
        {
            GameObject go = new GameObject(objectName);
            go.tag = "UICollider";

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

            UISliderRail uiSliderRail = go.AddComponent<UISliderRail>();
            uiSliderRail.transform.parent = parent;
            uiSliderRail.transform.localPosition = parentAnchor + relativeLocation;
            uiSliderRail.transform.localRotation = Quaternion.identity;
            uiSliderRail.transform.localScale = Vector3.one;
            uiSliderRail.width = width;
            uiSliderRail.height = height;
            uiSliderRail.thickness = thickness;
            uiSliderRail.margin = margin;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildHollowCubeEx(width, height, margin, thickness);
                //uiColorPickerSaturation.Anchor = Vector3.zero;
                //BoxCollider coll = go.GetComponent<BoxCollider>();
                //if (coll != null)
                //{
                //    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                //    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                //    if (initColliderSize.z < UIElement.collider_min_depth_deep)
                //    {
                //        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_deep / 2.0f);
                //        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_deep);
                //    }
                //    else
                //    {
                //        coll.center = initColliderCenter;
                //        coll.size = initColliderSize;
                //    }
                //    coll.isTrigger = true;
                //}
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(material);
                uiSliderRail.Color = c;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderRail;
        }
    }
}
