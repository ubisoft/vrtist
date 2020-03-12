using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class UIGrabber : UIElement
    {
        //public Deformer deformer = null;
        public GameObject prefab = null;
        public Color hoverColor = new Color(0f, 167f / 255f, 1f);
        //public Color baseColor = new Color(186f / 255f, 186f / 255f, 186 / 255f);

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public GameObjectHashChangedEvent onEnterUI3DObject = null;
        public GameObjectHashChangedEvent onExitUI3DObject = null;
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogWarning("No Prefab set for 3d Object.");
            }
            else
            {
                ToolsUIManager.Instance.RegisterUI3DObject(prefab);
            }

            onClickEvent.AddListener(OnPush3DObject);
            onReleaseEvent.AddListener(OnRelease3DObject);
            onHoverEvent.AddListener(OnStayOn3DObject);
        }

        public override void UpdateLocalPosition()
        {
            // just to avoid grabbing the UIElement shit, for the moment.
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                if (prefab != null)
                {
                    int hash = prefab.GetHashCode();
                    onEnterUI3DObject.Invoke(hash);
                }
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
                if (prefab != null)
                {
                    int hash = prefab.GetHashCode();
                    onExitUI3DObject.Invoke(hash);
                }
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (otherCollider.gameObject.name == "Cursor")
            {
                onHoverEvent.Invoke();
            }
        }

        public void OnPush3DObject()
        {
            SetColor(hoverColor);

            transform.localPosition += new Vector3(0f, 0f, -0.02f); // avance vers nous, dnas le repere de la page (local -Z)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        public void OnRelease3DObject()
        {
            SetColor(baseColor);

            transform.localPosition += new Vector3(0f, 0f, +0.02f); // recule, dnas le repere de la page (local +Z)
            transform.localScale = Vector3.one;
        }

        public void OnStayOn3DObject()
        {
            transform.localRotation *= Quaternion.Euler(0f, -3f, 0f); // rotate autour du Y du repere du parent (penche a 25, -35, 0)
        }

        // Handles multi-mesh and multi-material per mesh.
        public override void SetColor(Color color)
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Material[] materials = meshRenderer.materials;
                foreach (Material material in materials)
                {
                    material.SetColor("_BaseColor", color);
                }
            }
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            //Vector3 localWidgetPosition = transform.InverseTransformPoint(worldCursorColliderCenter);
            //Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);

            //float widthWithoutMargins = width - 2.0f * margin;
            //float startX = margin + widthWithoutMargins * sliderPositionBegin + railMargin;
            //float endX = margin + widthWithoutMargins * sliderPositionEnd - railMargin;

            //float snapXDistance = 0.002f;
            //// TODO: snap X if X if a bit left of startX or a bit right of endX.

            //if (localProjectedWidgetPosition.x > startX - snapXDistance && localProjectedWidgetPosition.x < endX + snapXDistance)
            //{
            //    // SNAP X left
            //    if (localProjectedWidgetPosition.x < startX)
            //        localProjectedWidgetPosition.x = startX;

            //    // SNAP X right
            //    if (localProjectedWidgetPosition.x > endX)
            //        localProjectedWidgetPosition.x = endX;

            //    // SNAP Y to middle
            //    localProjectedWidgetPosition.y = -height / 2.0f;

            //    float pct = (localProjectedWidgetPosition.x - startX) / (endX - startX);

            //    Value = minValue + pct * (maxValue - minValue); // will replace the slider cursor.

            //    // Haptic intensity as we go deeper into the widget.
            //    float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
            //    intensity *= intensity; // ease-in

            //    // TODO : Re-enable
            //    VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
            //}

            //Vector3 worldProjectedWidgetPosition = transform.TransformPoint(localProjectedWidgetPosition);
            //cursorShapeTransform.position = worldProjectedWidgetPosition;

            cursorShapeTransform.position = transform.position;
            cursorShapeTransform.rotation = transform.parent.rotation;
        }
    }
}
