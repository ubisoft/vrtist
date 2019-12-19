using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class PaletteCursor : MonoBehaviour
    {
        public GameObject tools = null;

        private Vector3 initialCursorLocalPosition = Vector3.zero;
        private bool isOnAWidget = false;
        private Transform widgetTransform = null;
        private UIElement widgetHit = null;

        void Start()
        {
            // Get the initial transform of the cursor mesh, in order to
            // be able to restore it when we go out of a widget.
            MeshFilter mf = GetComponentInChildren<MeshFilter>();
            GameObject go = mf.gameObject;
            Transform t = go.transform;
            Vector3 lp = t.localPosition;


            initialCursorLocalPosition = GetComponentInChildren<MeshFilter>().gameObject.transform.localPosition;

            ShowCursor(false);
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // Device rotation
                Vector3 position;
                Quaternion rotation;
                VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

                // The main cursor object always follows the controller
                // so that the collider sticks to the actual hand position.
                transform.localPosition = position;
                transform.localRotation = rotation;

                if (isOnAWidget)
                {
                    Transform cursorShapeTransform = GetComponentInChildren<MeshFilter>().gameObject.transform;
                    Vector3 localCursorColliderCenter = GetComponent<SphereCollider>().center;
                    Vector3 worldCursorColliderCenter = transform.TransformPoint(localCursorColliderCenter);

                    if (widgetHit != null && widgetHit.HandlesCursorBehavior())
                    {
                        widgetHit.HandleCursorBehavior(worldCursorColliderCenter, ref cursorShapeTransform);
                    }
                    else
                    { 
                        //Vector3 localWidgetPosition = widgetTransform.InverseTransformPoint(cursorShapeTransform.position);
                        Vector3 localWidgetPosition = widgetTransform.InverseTransformPoint(worldCursorColliderCenter);
                        Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);
                        Vector3 worldProjectedWidgetPosition = widgetTransform.TransformPoint(localProjectedWidgetPosition);
                        cursorShapeTransform.position = worldProjectedWidgetPosition;

                        // Haptic intensity as we go deeper into the widget.
                        float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                        intensity *= intensity; // ease-in
                        VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 
            if (other.gameObject.name == "Palette")
            {
                // deactivate all tools
                //tools.SetActive(false);
                ToolsUIManager.Instance.ShowTools(false);
                ShowCursor(true);
            }
            else if (other.gameObject.tag == "UICollider")
            {
                isOnAWidget = true;
                widgetTransform = other.transform;
                widgetHit = other.GetComponent<UIElement>();
                VRInput.SendHaptic(VRInput.rightController, 0.015f, 0.5f);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name == "Palette")
            {
                // reactivate all tools
                //tools.SetActive(true);
                ToolsUIManager.Instance.ShowTools(true);
                ShowCursor(false);
            }
            else if (other.gameObject.tag == "UICollider")
            {
                isOnAWidget = false;
                widgetTransform = null;
                widgetHit = null;
                GetComponentInChildren<MeshFilter>().gameObject.transform.localPosition = initialCursorLocalPosition;
            }
        }

        private void ShowCursor(bool doShow)
        {
            // NOTE: will not work if we add more objects under the cursor root.
            GetComponentInChildren<MeshRenderer>().enabled = doShow;
        }
    }
}