using System;
using UnityEngine;

namespace VRtist
{
    public class PaletteCursor : MonoBehaviour
    {
        public UIRay ray = null;

        private Vector3 initialCursorLocalPosition = Vector3.zero;
        private bool isOnAWidget = false;
        private Transform widgetTransform = null;
        private UIElement widgetHit = null;
        private AudioSource audioClick = null;

        private int currentShapeId = 0;
        private int previousShapeId = 0;
        private Transform currentShapeTransform = null;
        private Transform arrowCursor = null;
        private Transform grabberCursor = null;
        private IDisposable UIEnabled = null;
        private bool paletteEnabled = true;

        private bool lockedOnAWidget = false;
        private bool isOutOfWidget = true;
        private bool isOutOfVolume = true;

        UIElement prevWidget = null; // for RAY

        void Start()
        {
            // Get the initial transform of the cursor mesh, in order to
            // be able to restore it when we go out of a widget.
            MeshFilter mf = GetComponentInChildren<MeshFilter>(true);
            GameObject go = mf.gameObject;
            Transform t = go.transform;
            Vector3 lp = t.localPosition;

            audioClick = GetComponentInChildren<AudioSource>(true);

            initialCursorLocalPosition = GetComponentInChildren<MeshFilter>(true).gameObject.transform.localPosition;

            arrowCursor = transform.Find("Arrow");
            grabberCursor = transform.Find("Grabber");
            SetCursorShape(0); // arrow
            HideAllCursors();

            Utils.OnPrefabInstantiated += OnPrefabInstantiated;
        }

        void Update()
        {
            if (!VRInput.TryGetDevices())
                return;

            // Device rotation
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

            // The main cursor object always follows the controller
            // so that the collider sticks to the actual hand position.
            transform.localPosition = position;
            transform.localRotation = rotation;

            if (!UIElement.UIEnabled.Value)
                return;

            if (isOnAWidget || lockedOnAWidget)
            {
                Vector3 localCursorColliderCenter = GetComponent<SphereCollider>().center;
                Vector3 worldCursorColliderCenter = transform.TransformPoint(localCursorColliderCenter);

                if (widgetHit != null && widgetHit.HandlesCursorBehavior())
                {
                    widgetHit.HandleCursorBehavior(worldCursorColliderCenter, ref currentShapeTransform);
                    // TODO: est-ce qu'on gere le son dans les widgets, ou au niveau du curseur ???
                    //       On peut faire ca dans le HandleCursorBehavior().
                    //audioClick.Play();
                }
                else
                { 
                    //Vector3 localWidgetPosition = widgetTransform.InverseTransformPoint(cursorShapeTransform.position);
                    Vector3 localWidgetPosition = widgetTransform.InverseTransformPoint(worldCursorColliderCenter);
                    Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);
                    Vector3 worldProjectedWidgetPosition = widgetTransform.TransformPoint(localProjectedWidgetPosition);
                    currentShapeTransform.position = worldProjectedWidgetPosition;

                    // Haptic intensity as we go deeper into the widget.
                    float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                    intensity *= intensity; // ease-in
                    if (UIElement.UIEnabled.Value)
                        VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
                }
            }

            HandleRaycast();
        }

        private void FixedUpdate()
        {
            //HandleRaycast();
        }

        private void HandleRaycast()
        {
            RaycastHit[] hits;
            Vector3 worldStart = transform.TransformPoint(0.0104f, 0, 0.065f);
            Vector3 worldEnd = transform.TransformPoint(0.0104f, 0, 1f);
            Vector3 worldDirection = worldEnd - worldStart;
            Ray r = new Ray(worldStart, worldDirection);
            int layersMask = LayerMask.GetMask(new string[] { "UI" });
            hits = Physics.RaycastAll(r, 3.0f, layersMask, QueryTriggerInteraction.Collide);
            if (hits.Length > 0)
            {
                bool volumeIsHit = false;
                bool widgetIsHit = false;
                bool handleIsHit = false;

                float closestVolumeDistance = Mathf.Infinity;
                float closestWidgetDistance = Mathf.Infinity;
                float closestHandleDistance = Mathf.Infinity;

                Vector3 volumeCollisionPoint = Vector3.zero;
                Vector3 widgetCollisionPoint = Vector3.zero;
                Vector3 handleCollisionPoint = Vector3.zero;

                UIElement widget = null;
                UIHandle handle = null;
                UIVolumeTag volume = null;

                // Find if a volume/handle/widget has been hit, and compute the closest hit distance/point.
                for (int i = 0; i < hits.Length; ++i)
                {
                    Transform hit = hits[i].transform;

                    UIVolumeTag volumeHit = hit.GetComponent<UIVolumeTag>();
                    if (volumeHit != null)
                    {
                        volumeIsHit = true;
                        if (volume != null || hits[i].distance < closestVolumeDistance)
                        {
                            volume = volumeHit;
                            volumeCollisionPoint = hits[i].point; // world space
                            closestVolumeDistance = hits[i].distance;
                        }
                    }

                    UIHandle handleHit = hit.GetComponent<UIHandle>();
                    if (handleHit != null)
                    {
                        handleIsHit = true;
                        if (handle != null || hits[i].distance < closestHandleDistance)
                        {
                            handle = handleHit;
                            handleCollisionPoint = hits[i].point; // world space
                            closestHandleDistance = hits[i].distance;
                        }
                    }

                    UIElement widgetHit = hit.GetComponent<UIElement>();
                    if (widgetHit != null)
                    {
                        widgetIsHit = true;
                        if (widget != null || hits[i].distance < closestWidgetDistance)
                        {
                            widget = widgetHit;
                            widgetCollisionPoint = hits[i].point; // world space
                            closestWidgetDistance = hits[i].distance;
                        }
                    }
                }

                if (handleIsHit)
                {
                    ray.gameObject.SetActive(true);
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(handleCollisionPoint);

                    ray.SetHandleColor();

                    ExitPreviousWidget();
                }
                else if (widgetIsHit)
                {
                    ray.gameObject.SetActive(true);
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(widgetCollisionPoint);

                    if (widget.GetComponent<UIPanel>())
                    {
                        ray.SetPanelColor();
                    }
                    else
                    {
                        ray.SetWidgetColor();
                        if (prevWidget != widget)
                        {
                            if (prevWidget != null)
                            {
                                prevWidget.OnRayExit();
                            }

                            widget.OnRayEnter();

                            prevWidget = widget;
                        }
                        else
                        {
                            widget.OnRayHover();
                        }
                    }
                }
                else if (volumeIsHit)
                {
                    ray.gameObject.SetActive(true);
                    ray.SetStartPosition(worldStart);
                    ray.SetEndPosition(worldEnd); // volumeCollisionPoint
                    ray.SetVolumeColor();

                    ExitPreviousWidget();
                }
                else // does it happen??? -> layer UI but neither UIVolumeTag nor UIElement
                {
                    ray.gameObject.SetActive(false);
                    ExitPreviousWidget();
                }
            }
            else
            {
                ray.gameObject.SetActive(false);
                ExitPreviousWidget();
            }
        }

        private void ExitPreviousWidget()
        {
            if (prevWidget != null)
            {
                prevWidget.OnRayExit();
                prevWidget = null;
            }
        }

        private void ReleaseUIEnabledGuard()
        {
            if (null != UIEnabled)
            {
                UIEnabled.Dispose();
                UIEnabled = null;
            }
        }

        void OnPrefabInstantiated(object sender, PrefabInstantiatedArgs args)
        {
            ResetCursor();
        }

        private void ResetCursor()
        {
            ReleaseUIEnabledGuard();
            ToolsUIManager.Instance.ShowTools(true);
            HideAllCursors();
        }

        private void OnTriggerStay(Collider other)
        {
            if (lockedOnAWidget)
                return;

            if (other.GetComponent<UIVolumeTag>() != null)
            {
                isOutOfVolume = false;

                if (CommandManager.IsUndoGroupOpened())
                {
                    if (null == UIEnabled)
                    {
                        UIEnabled = UIElement.UIEnabled.SetValue(false);
                        if (null != widgetHit)
                        {
                            UIGrabber grabber = widgetHit.GetComponent<UIGrabber>();
                            grabber.OnRelease3DObject();
                        }                        
                    }                    
                    return;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (lockedOnAWidget)
                return;

            if (other.GetComponent<UIVolumeTag>() != null)
            {
                isOutOfVolume = false;

                if (CommandManager.IsUndoGroupOpened())
                {
                    if(null == UIEnabled)
                        UIEnabled = UIElement.UIEnabled.SetValue(false);
                    return;
                }

                // deactivate all tools
                //tools.SetActive(false);
                ToolsUIManager.Instance.ShowTools(false);
                SetCursorShape(0); // arrow
            }
            else if (other.gameObject.tag == "UICollider")
            {
                isOnAWidget = true;
                widgetTransform = other.transform;
                widgetHit = other.GetComponent<UIElement>();
                if (UIElement.UIEnabled.Value)
                {
                    VRInput.SendHaptic(VRInput.rightController, 0.015f, 0.5f);
                    audioClick.Play();
                }

                isOutOfWidget = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<UIVolumeTag>() != null)
            {
                if (!lockedOnAWidget)
                {
                    ResetCursor();
                }
                isOutOfVolume = true;
            }
            else if (other.gameObject.tag == "UICollider")
            {
                if (!lockedOnAWidget)
                {
                    ReleaseWidget();
                }
                isOutOfWidget = true;
            }
        }

        private void ReleaseWidget()
        {
            isOnAWidget = false;
            widgetTransform = null;
            widgetHit = null;
            MeshFilter meshFilter = GetComponentInChildren<MeshFilter>(true);
            if (null != meshFilter)
            {
                meshFilter.gameObject.transform.localPosition = initialCursorLocalPosition;
            }
            SetCursorShape(0);
        }

        public bool IsLockedOnWidget()
        {
            return lockedOnAWidget;
        }

        public bool IsLockedOnThisWidget(Transform other)
        {
            return widgetTransform == other;
        }

        public void LockOnWidget(bool value)
        {
            if (value == lockedOnAWidget)
                return;

            if (value)
            {
                lockedOnAWidget = true;
            }
            else
            {
                lockedOnAWidget = false;
                if (isOutOfWidget)
                {
                    ReleaseWidget();
                }
                if (isOutOfVolume)
                {
                    ResetCursor();
                }
            }
        }

        // 0: arrow, 1: box
        private void SetCursorShape(int shape)
        {
            currentShapeId = shape;

            HideAllCursors();
            switch (shape)
            {
                case 0: arrowCursor.gameObject.SetActive(true); currentShapeTransform = arrowCursor.transform; break;
                case 1: grabberCursor.gameObject.SetActive(true); currentShapeTransform = grabberCursor.transform; break;
            }
        }
        
        public void PushCursorShape(int shape)
        {
            previousShapeId = currentShapeId;
            SetCursorShape(shape);
        }

        public void PopCursorShape()
        {
            SetCursorShape(previousShapeId);
        }        

        private void HideAllCursors()
        {
            arrowCursor.gameObject.SetActive(false);
            grabberCursor.gameObject.SetActive(false);

            //MeshRenderer[] rr = GetComponentsInChildren<MeshRenderer>();
            //foreach (MeshRenderer r in rr)
            //{
            //    r.enabled = false;
            //}
        }
    }
}