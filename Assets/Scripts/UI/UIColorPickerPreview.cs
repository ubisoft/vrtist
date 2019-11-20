using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerPreview : MonoBehaviour
    {
        // UIElement ?

        private float width = 1.0f;
        private float height = 1.0f;
        private float thickness = 1.0f;

        public void SetColor(Color color)
        {
            GetComponent<MeshRenderer>().material.SetColor("_Color", color);
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(newWidth, newHeight, newThickness);
            theNewMesh.name = "UIColorPickerPreview_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            UpdateColliderDimensions();
        }

        public void UpdateColliderDimensions()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            BoxCollider coll = gameObject.GetComponent<BoxCollider>();
            if (meshFilter != null && coll != null)
            {
                Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                if (initColliderSize.z < UIElement.collider_min_depth_deep)
                {
                    coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_deep / 2.0f);
                    coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_deep);
                }
                else
                {
                    coll.center = initColliderCenter;
                    coll.size = initColliderSize;
                }
            }
        }

        public static UIColorPickerPreview CreateUIColorPickerPreview(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            Material material)
        {
            GameObject go = new GameObject(objectName);
            go.tag = "UIObject";

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
            //uiColorPickerPreview.relativeLocation = relativeLocation;
            uiColorPickerPreview.transform.parent = parent;
            uiColorPickerPreview.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPickerPreview.transform.localRotation = Quaternion.identity;
            uiColorPickerPreview.transform.localScale = Vector3.one;
            uiColorPickerPreview.width = width;
            uiColorPickerPreview.height = height;
            uiColorPickerPreview.thickness = thickness;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(width, height, thickness);
                //uiColorPickerPreview.Anchor = Vector3.zero;
                BoxCollider coll = go.GetComponent<BoxCollider>();
                if (coll != null)
                {
                    Vector3 initColliderCenter = meshFilter.sharedMesh.bounds.center;
                    Vector3 initColliderSize = meshFilter.sharedMesh.bounds.size;
                    if (initColliderSize.z < UIElement.collider_min_depth_deep)
                    {
                        coll.center = new Vector3(initColliderCenter.x, initColliderCenter.y, UIElement.collider_min_depth_deep / 2.0f);
                        coll.size = new Vector3(initColliderSize.x, initColliderSize.y, UIElement.collider_min_depth_deep);
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
            if (meshRenderer != null && material != null)
            {
                meshRenderer.sharedMaterial = Instantiate(material);
            }

            return uiColorPickerPreview;
        }
    }
}
