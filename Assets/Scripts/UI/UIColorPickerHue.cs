using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(BoxCollider))]
    public class UIColorPickerHue : MonoBehaviour
    {
        // UIElement ?

        private float width = 1.0f;
        private float height = 1.0f;
        private float thickness = 1.0f;

        public UIColorPicker colorPicker = null;
        float cursorPosition = 0.5f; // normalized position

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

        public float GetHue()
        {
            return cursorPosition;
        }

        // value: [0..1]
        public void SetHue(float value)
        {
            cursorPosition = value;
            cursor.localPosition = new Vector3(width * value, -height/2.0f, 0);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.name != "Cursor")
                return;

            Vector3 colliderSphereCenter = other.gameObject.GetComponent<SphereCollider>().center;
            colliderSphereCenter = other.gameObject.transform.localToWorldMatrix.MultiplyPoint(colliderSphereCenter);

            Vector3 position = transform.worldToLocalMatrix.MultiplyPoint(colliderSphereCenter);

            SetHue(Mathf.Clamp(position.x / width, 0, 1));
            colorPicker.OnColorChanged();
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildBoxEx(newWidth, newHeight, newThickness);
            theNewMesh.name = "UIColorPickerHue_GeneratedMesh";
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

        public static UIColorPickerHue CreateUIColorPickerHue(
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

            UIColorPickerHue uiColorPickerHue = go.AddComponent<UIColorPickerHue>();
            //uiColorPickerHue.relativeLocation = relativeLocation;
            uiColorPickerHue.transform.parent = parent;
            uiColorPickerHue.transform.localPosition = parentAnchor + relativeLocation;
            uiColorPickerHue.transform.localRotation = Quaternion.identity;
            uiColorPickerHue.transform.localScale = Vector3.one;
            uiColorPickerHue.width = width;
            uiColorPickerHue.height = height;
            uiColorPickerHue.thickness = thickness;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildBoxEx(width, height, thickness);
                //uiColorPickerHue.Anchor = Vector3.zero;
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
            cursor.transform.parent = uiColorPickerHue.transform;
            cursor.transform.localPosition = Vector3.zero;
            uiColorPickerHue.cursor = cursor.transform;

            return uiColorPickerHue;
        }
    }
}
