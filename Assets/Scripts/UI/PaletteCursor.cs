using System;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class PaletteCursor : MonoBehaviour
    {
        private UIRay ray = null;

        [Range(0, 1)]
        public float rayStiffness = 0.5f;

        private UIElement widgetClicked = null;

        private AudioSource audioClickIn = null;
        private AudioSource audioClickOut = null;

        private IDisposable uiEnabledGuard = null;

        UIElement prevWidget = null; // for RAY
        Vector3 prevWorldDirection = Vector3.zero;

        void Start()
        {
            audioClickIn = transform.Find("Audio_ClickIn").GetComponent<AudioSource>();
            audioClickOut = transform.Find("Audio_ClickOut").GetComponent<AudioSource>();

            ray = GetComponentInChildren<UIRay>();
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

            Vector3 worldStart = transform.TransformPoint(0.0104f, 0, 0.065f);
            Vector3 worldEnd = transform.TransformPoint(0.0104f, 0, 1f);
            Vector3 newWorldDirection = worldEnd - worldStart;
            Vector3 worldDirection = prevWorldDirection != Vector3.zero ? Vector3.Lerp(prevWorldDirection, newWorldDirection, rayStiffness) : newWorldDirection;
            worldDirection.Normalize();
            prevWorldDirection = worldDirection;
            Vector3 rayEndPoint = worldEnd;

            Ray r = new Ray(worldStart, worldDirection);

            if (UIElement.UIEnabled.Value)
            {
                // If a widget is locked (trigger has been pressed on it), give it a chance to handle the ray endpoint.
                if (widgetClicked != null && widgetClicked.OverridesRayEndPoint())
                {
                    widgetClicked.OverrideRayEndPoint(r, ref rayEndPoint);
                }
            }

            // Lambda to avoid copy-paste.
            System.Action handleHitNothing = () =>
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
            };

            //
            // First, try to hit anything, in order to findout if we hit a Non-UI object first.
            //

            RaycastHit hitInfo;
            int allLayersMask = -1; // ~0
            if (!Physics.Raycast(r, out hitInfo, 3.0f, allLayersMask, QueryTriggerInteraction.Collide)
                || (hitInfo.transform.gameObject.layer != LayerMask.NameToLayer("UI")
                && hitInfo.transform.gameObject.layer != LayerMask.NameToLayer("SelectionUI")
                && hitInfo.transform.gameObject.layer != LayerMask.NameToLayer("HoverUI")))
            {
                // Nothing hit, or hit a non-UI object.
                handleHitNothing();
                return;
            }
            else
            {
                // detect if the first ray was shoot from inside an object

                Ray backRay = new Ray(hitInfo.point - 0.01f * worldDirection, -worldDirection);
                float d = hitInfo.distance - 0.01f;
                bool raycastOK = Physics.Raycast(backRay, out hitInfo, hitInfo.distance, allLayersMask, QueryTriggerInteraction.Collide);
                if (raycastOK && hitInfo.distance < d)
                {
                    handleHitNothing();
                    return;
                }
            }

            //
            // Raycast ALL UI elements
            //

            RaycastHit[] hits;
            int layersMask = LayerMask.GetMask(new string[] { "UI", "SelectionUI", "HoverUI" });
            hits = Physics.RaycastAll(r, 3.0f, layersMask, QueryTriggerInteraction.Collide);
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
                                if (!widgetClicked.IgnoreRayInteraction())
                                    widgetClicked.OnRayEnterClicked(); // act as if we re-click on widget.
                            }
                            else // click has been pushed on another widget
                            {
                                if (prevWidget == widgetClicked)
                                {
                                    if (!widgetClicked.IgnoreRayInteraction())
                                        widgetClicked.OnRayExitClicked();
                                }
                                // dont do anything for the new widget, not even hover.
                            }
                        }
                        else // no click, simple hover
                        {
                            if (prevWidget != null)
                            {
                                if (!prevWidget.IgnoreRayInteraction())
                                    prevWidget.OnRayExit();
                            }

                            if (!widget.IgnoreRayInteraction())
                                widget.OnRayEnter();
                        }
                    }
                    else // still on same widget
                    {
                        if (widgetClicked != null) // trigger is held pushed.
                        {
                            if (widgetClicked == widget) // on same widget
                            {
                                if (!widgetClicked.IgnoreRayInteraction())
                                    widgetClicked.OnRayHoverClicked();
                            }
                            else // still hovering a widget which is not the one clicked.
                            {
                                // TODO: should we add another state here? this is a FAKE hover.
                                //       we want to show that this was the clicked widget but the ray is elsewhere.
                                //if (!widgetClicked.IgnoreRayInteraction())
                                //widgetClicked.OnRayHover(); // simple hover without the click effect.

                                // do nothing for the new widget.
                            }
                        }
                        else
                        {
                            if (!widget.IgnoreRayInteraction())
                                widget.OnRayHover();
                        }
                    }

                    // "Just click" is independant of whether we stay or change hit widget.
                    if (triggerJustClicked)
                    {
                        if (!widget.IgnoreRayInteraction())
                        {
                            widget.OnRayClick();
                            audioClickIn.Play();
                            UIElement.ClickHapticFeedback(); // TODO: voir si on le met individuellement dans chaque widget avec des exceptions.
                        }

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
                                if (!widget.IgnoreRayInteraction())
                                {
                                    widget.OnRayReleaseInside();
                                    audioClickOut.Play();
                                    UIElement.ClickHapticFeedback();
                                }
                            }
                            else
                            {
                                // clear state of previously clicked widget
                                if (!widgetClicked.IgnoreRayInteraction())
                                {
                                    widgetClicked.OnRayReleaseOutside();
                                    //audioClickOut.Play();
                                    //UIElement.ClickHapticFeedback();
                                }

                                // give the new widget a chance to play some OnHover animation.
                                if (!widget.IgnoreRayInteraction())
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
                else // Layer UI but neither UIVolumeTag nor UIElement ==> Grid or Tool Mouthpiece.
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
                handleHitNothing();
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
                        if (!widgetClicked.IgnoreRayInteraction())
                            widgetClicked.OnRayExitClicked();
                    }
                }

                if (triggerJustReleased)
                {
                    if (!widgetClicked.IgnoreRayInteraction())
                    {
                        widgetClicked.OnRayReleaseOutside(); // just UN-push, no events triggered.
                        //audioClickOut.Play();
                        //UIElement.ClickHapticFeedback();
                    }

                    widgetClicked = null;
                }
            }
            else // no click, simple hover
            {
                if (prevWidget != null)
                {
                    if (!prevWidget.IgnoreRayInteraction())
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
    }
}