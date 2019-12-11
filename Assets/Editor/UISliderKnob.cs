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
    public class UISliderKnob : MonoBehaviour
    {
        public float headWidth;
        public float headHeight;
        public float headDepth;
        public float footWidth;
        public float footHeight;
        public float footDepth;

        // TODO: OnValidate() on width/heigt/etc... ??? Controlled by UISlider already.

        private Color _color;
        public Color Color { get { return _color; } set { _color = value; ApplyColor(_color); } }

        void Awake()
        {
            if (EditorApplication.isPlaying || Application.isPlaying)
            {
                // Why did I copy these lines from the ColorPicker???

                //width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                //height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                //thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
                //margin = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public void RebuildMesh(float newHeadWidth, float newHeadHeight, float newHeadDepth, 
                                float newFootWidth, float newFootHeight, float newFootDepth)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildSliderKnobEx(newHeadWidth, newHeadHeight, newHeadDepth, newFootWidth, newFootHeight, newFootDepth);
            theNewMesh.name = "UISliderKnob_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            headWidth = newHeadWidth;
            headHeight = newHeadHeight;
            headDepth = newHeadDepth;
            footWidth = newFootWidth;
            footHeight = newFootHeight;
            footDepth = newFootDepth;

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

        public static UISliderKnob CreateUISliderKnob(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float head_width,
            float head_height,
            float head_depth,
            float foot_width,
            float foot_height,
            float foot_depth,
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

            UISliderKnob uiSliderKnob = go.AddComponent<UISliderKnob>();
            uiSliderKnob.transform.parent = parent;
            uiSliderKnob.transform.localPosition = parentAnchor + relativeLocation;
            uiSliderKnob.transform.localRotation = Quaternion.identity;
            uiSliderKnob.transform.localScale = Vector3.one;
            uiSliderKnob.headWidth = head_width;
            uiSliderKnob.headHeight = head_height;
            uiSliderKnob.headDepth = head_depth;
            uiSliderKnob.footWidth = foot_width;
            uiSliderKnob.footHeight = foot_height;
            uiSliderKnob.footDepth = foot_depth;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildSliderKnobEx(head_width, head_height, head_depth, foot_width, foot_height, foot_depth);
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
                uiSliderKnob.Color = c;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderKnob;
        }
    }
}
