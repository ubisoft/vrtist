using System;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class PaletteCursor : MonoBehaviour
    {
        private UIRay ray = null;
        [Range(0,1)]
        public float rayStiffness = 0.5f;

        private Vector3 initialCursorLocalPosition = Vector3.zero;
        private bool isOnAWidget = false;
        private Transform widgetTransform = null;
        private UIElement widgetHit = null;

        private UIElement widgetClicked = null;

        private AudioSource audioClick = null;

        private int currentShapeId = 0;
        private int previousShapeId = 0;
        private Transform currentShapeTransform = null;
        private Transform arrowCursor = null;
        private Transform grabberCursor = null;
        private IDisposable uiEnabledGuard = null;
        private bool paletteEnabled = true;

        private bool lockedOnAWidget = false;
        private bool isOutOfWidget = true;
        private bool isOutOfVolume = true;

        UIElement prevWidget = null; // for RAY
        Vector3 prevWorldDirection = Vector3.zero;

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

            ray = GetComponentInChildren<UIRay>();

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

            if (ray != null)
            {
                HandleRaycast();
            }
        }

        private void ActivateRay(bool value)
        {
            ToolsUIManager.Instance.ShowTools(!value);
            ray.gameObject.SetActive(value);
        }

        private void HandleRaycast()
        {
            // TODO:
            // - audioClick.Play(); ????
            // - VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity); ???

            //
            // Find out if the trigger button was pressed, in order to send the info the the widget hit.
            //

            bool triggerJustClicked = false;
            bool triggerJustReleased = false;
            VRInput.GetInstantButtonEvent(VRInput.rightController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

            //
            // Raycast, find out the closest volume, handle and widget.
            //

            RaycastHit[] hits;
            Vector3 worldStart = transform.TransformPoint(0.0104f, 0, 0.065f);
            Vector3 worldEnd = transform.TransformPoint(0.0104f, 0, 1f);
            Vector3 newWorldDirection = worldEnd - worldStart;
            Vector3 worldDirection = prevWorldDirection != Vector3.zero ? Vector3.Lerp(prevWorldDirection, newWorldDirection, rayStiffness) : newWorldDirection;
            worldDirection.Normalize();
            prevWorldDirection = worldDirection;

            Ray r = new Ray(worldStart, worldDirection);
            int layersMask = LayerMask.GetMask(new string[] { "UI" });
            hits = Physics.RaycastAll(r, 3.0f, layersMask, QueryTriggerInteraction.Collide);

            // If a widget is locked (trigger has been pressed on it), give it a chance to handle the ray endpoint.
            Vector3 rayEndPoint = worldEnd;
            if (UIElement.UIEnabled.Value)
            {
                if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                {
                    widgetClicked.OverrideRayEndPoint(r, ref rayEndPoint);
                }
            }

            if (hits.Length > 0)
            {
                if (!UIElement.UIEnabled.Value)
                    return;

                // Ray hits anything UI, but a tool action is happening.
                // Create a guard to disable any action if not already created.
                if (CommandManager.IsUndoGroupOpened())
                {
                    if (null == uiEnabledGuard)
                    {
                        uiEnabledGuard = UIElement.UIEnabled.SetValue(false);
                    }
                    ActivateRay(false);
                    return;
                }

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

                //
                // Find if a volume/handle/widget has been hit, and compute the closest hit distance/point.
                //

                for (int i = 0; i < hits.Length; ++i)
                {
                    Transform hit = hits[i].transform;

                    UIVolumeTag volumeHit = hit.GetComponent<UIVolumeTag>();
                    if (volumeHit != null)
                    {
                        volumeIsHit = true;
                        if (hits[i].distance < closestVolumeDistance)
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
                        if (hits[i].distance < closestHandleDistance)
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
                        if (hits[i].distance < closestWidgetDistance)
                        {
                            widget = widgetHit;
                            widgetCollisionPoint = hits[i].point; // world space
                            closestWidgetDistance = hits[i].distance;
                        }
                    }
                }



                //
                // Send messages and states to the widget hit, with priorities.
                //


                //
                // TODO
                //
                // REFACTOR this horrible code.
                //
                // state machine?
                //

                if (handleIsHit)
                {
                    if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                    {
                        ActivateRay(true);
                        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                    }
                    else
                    {
                        ActivateRay(false);
                    }
                    HandleRayOutOfWidget(triggerJustReleased);
                }
                else if (widgetIsHit)
                {
                    if (prevWidget != widget) // change widget
                    {
                        if (widgetClicked != null) // trigger is held pushed.
                        {
                            if (widgetClicked == widget) // on same widget
                            {
                                widgetClicked.OnRayEnterClicked(); // act as if we re-click on widget.
                            }
                            else // click has been pushed on another widget
                            {
                                if (prevWidget == widgetClicked)
                                {
                                    widgetClicked.OnRayExitClicked();
                                }
                                // dont do anything for the new widget, not even hover.
                            }
                        }
                        else // no click, simple hover
                        {
                            if (prevWidget != null)
                            {
                                prevWidget.OnRayExit();
                            }

                            widget.OnRayEnter();
                        }
                    }
                    else // still on same widget
                    {
                        if (widgetClicked != null) // trigger is held pushed.
                        {
                            if (widgetClicked == widget) // on same widget
                            {
                                widgetClicked.OnRayHoverClicked();
                            }
                            else // still hovering a widget which is not the one clicked.
                            {
                                // TODO: should we add another state here? this is a FAKE hover.
                                //       we want to show that this was the clicked widget but the ray is elsewhere.
                                
                                //widgetClicked.OnRayHover(); // simple hover without the click effect.

                                // do nothing for the new widget.
                            }
                        }
                        else
                        {
                            widget.OnRayHover();
                        }
                    }

                    // "Just click" is independant of whether we stay or change hit widget.
                    if (triggerJustClicked)
                    {
                        widget.OnRayClick();
                        widgetClicked = widget;
                        if (widgetClicked.OverridesRayEndPoint())
                        {
                            // call this here when the "triggerJustClicked" state of VRInput is still set.
                            widgetClicked.OverrideRayEndPoint(r, ref rayEndPoint);
                        }
                    }

                    // I prefer treating "Just released" outside of the rest.
                    // BUT this leads to maybe 2 events sent for the same widget.
                    if (triggerJustReleased)
                    {
                        // do not send Release to another widget than the one which received the click.
                        if (widgetClicked != null)
                        {
                            if (widgetClicked == widget)
                            {
                                widget.OnRayReleaseInside();
                            }
                            else
                            {
                                // clear state of previously clicked widget
                                widgetClicked.OnRayReleaseOutside();
                                // give the new widget a chance to play some OnHover animation.
                                widget.OnRayEnter();
                            }
                        }
                        else
                        {
                            Debug.LogError("Just Released received without having clicked before on any widget!!");
                        }

                        widgetClicked = null;
                    }

                    prevWidget = widget; // even if the same.

                    ActivateRay(true);

                    if (widget.GetComponent<UIPanel>())
                    {
                        ray.SetPanelColor();
                    }
                    else
                    {
                        ray.SetWidgetColor();
                    }

                    if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                    {
                        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                    }
                    else
                    {
                        ray.SetParameters(worldStart, widgetCollisionPoint, newWorldDirection);
                    }
                }
                else if (volumeIsHit)
                {
                    ray.gameObject.SetActive(true);
                    if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                    {
                        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                    }
                    else
                    {
                        ray.SetVolumeColor();
                        ray.SetParameters(worldStart, worldStart + worldDirection * 0.3f, newWorldDirection); // volumeCollisionPoint
                    }

                    HandleRayOutOfWidget(triggerJustReleased);
                }
                else // Layer UI but neither UIVolumeTag nor UIElement == Grid, for example.
                {
                    if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                    {
                        ActivateRay(true);
                        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                    }
                    else
                    {
                        ActivateRay(false);
                    }
                    HandleRayOutOfWidget(triggerJustReleased);
                }
            }
            else // No collision, most common case.
            {
                if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                {
                    ActivateRay(true);
                    ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                }
                else
                {
                    ActivateRay(false);
                    ReleaseUIEnabledGuard(); // release the guard if there was one.
                }
                HandleRayOutOfWidget(triggerJustReleased);
            }
        }

        private void HandleRayOutOfWidget(bool triggerJustReleased)
        {
            if (widgetClicked != null) // trigger is held pushed.
            {
                if (prevWidget != null) // do it only once.
                {
                    if (prevWidget == widgetClicked)
                    {
                        widgetClicked.OnRayExitClicked();
                    }
                }

                if (triggerJustReleased)
                {
                    widgetClicked.OnRayReleaseOutside(); // just UN-push, no events triggered.
                    widgetClicked = null;
                }
            }
            else // no click, simple hover
            {
                if (prevWidget != null)
                {
                    prevWidget.OnRayExit();
                }
            }

            prevWidget = null;
        }

        private void ReleaseUIEnabledGuard()
        {
            if (null != uiEnabledGuard)
            {
                uiEnabledGuard.Dispose();
                uiEnabledGuard = null;
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
                    if (null == uiEnabledGuard)
                    {
                        uiEnabledGuard = UIElement.UIEnabled.SetValue(false);
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
                    if(null == uiEnabledGuard)
                        uiEnabledGuard = UIElement.UIEnabled.SetValue(false);
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
            HideAllCursors(); // no we use the RAY.

            // OLD CODE

            //currentShapeId = shape;

            //HideAllCursors();
            //switch (shape)
            //{
            //    case 0: arrowCursor.gameObject.SetActive(true); currentShapeTransform = arrowCursor.transform; break;
            //    case 1: grabberCursor.gameObject.SetActive(true); currentShapeTransform = grabberCursor.transform; break;
            //}
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
        }
    }
}