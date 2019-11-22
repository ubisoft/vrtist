using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerSaturation : MonoBehaviour
    {
        // UIElement ?

        private float width = 1.0f;
        private float height = 1.0f;
        private float thickness = 1.0f;

        public UIColorPicker colorPicker = null;

        Color baseColor;
        Vector2 cursorPosition = new Vector2(0.5f, 0.5f); // normalized

        public Transform cursor;

        void Awake()
        {
            if (EditorApplication.isPlaying || Application.isPlaying)
            {
                colorPicker = GetComponentInParent<UIColorPicker>();
                width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public void SetBaseColor(Color clr)
        {
            baseColor = clr;
            var renderer = GetComponent<MeshRenderer>();
            renderer.material.SetColor("_Color", clr);
        }

        public Vector2 GetSaturation()
        {
            return cursorPosition;
        }

        public void SetSaturation(Vector2 sat)
        {
            cursorPosition = sat;
            cursor.localPosition = new Vector3(width * sat.x, -height * (1.0f-sat.y), 0);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.name != "Cursor")
                return;

            Vector3 colliderSphereCenter = other.gameObject.GetComponent<SphereCollider>().center;
            colliderSphereCenter = other.gameObject.transform.localToWorldMatrix.MultiplyPoint(colliderSphereCenter);

            Vector3 position = transform.worldToLocalMatrix.MultiplyPoint(colliderSphereCenter);

            float x = position.x / width;
            float y = 1.0f - (-position.y / height);
            x = Mathf.Clamp(x, 0, 1);
            y = Mathf.Clamp(y, 0, 1);
            SetSaturation(new Vector2(x, y));

            colorPicker.OnColorChanged();
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(newWidth, newHeight, newThickness);
            theNewMesh.name = "UIColorPickerSaturation_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;

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
        
        public static UIColorPickerSaturation CreateUIColorPickerSaturation(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            Material material,
            GameObject cursorPrefab)
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

            UIColorPickerSaturation uiColorPickerSaturation = go.AddComponent<UIColorPickerSaturation>();
            //uiColorPickerSaturation.relativeLocation = relativeLocation;
            uiColorPickerSaturation.transform.parent = parent;
            uiColorPickerSaturation.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPickerSaturation.transform.localRotation = Quaternion.identity;
            uiColorPickerSaturation.transform.localScale = Vector3.one;
            uiColorPickerSaturation.width = width;
            uiColorPickerSaturation.height = height;
            uiColorPickerSaturation.thickness = thickness;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(width, height, thickness);
                //uiColorPickerSaturation.Anchor = Vector3.zero;
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

            // Add a cursor
            GameObject cursor = Instantiate<GameObject>(cursorPrefab);
            cursor.transform.parent = uiColorPickerSaturation.transform;
            cursor.transform.localPosition = Vector3.zero;
            uiColorPickerSaturation.cursor = cursor.transform;

            return uiColorPickerSaturation;
        }
    }
}
