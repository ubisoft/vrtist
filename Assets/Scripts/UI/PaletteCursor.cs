﻿/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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

        private IDisposable uiEnabledGuard = null;

        private AudioSource audioSource;

        UIElement prevWidget = null; // for RAY
        GameObject hoveredObject = null;
        Vector3 prevWorldDirection = Vector3.zero;

        bool WidgetIsClicked { get { return widgetClicked != null; } }
        bool TitleBarIsGripped { get { return ToolsManager.CurrentToolIsGripping(); } }

        void Start()
        {
            audioSource = transform.Find("AudioSource").GetComponent<AudioSource>();
            ray = GetComponentInChildren<UIRay>();
        }

        void Update()
        {
            if (!VRInput.TryGetDevices())
                return;

            // Device rotation
            //VRInput.GetControllerTransform(VRInput.primaryController, out Vector3 position, out Quaternion rotation);
            Transform laserTransform = GlobalState.GetControllerLaser();

            // The main cursor object always follows the controller
            // so that the collider sticks to the actual hand position.
            transform.position = laserTransform.position;
            transform.rotation = laserTransform.rotation;

            if (null != ray)
            {
                if (GlobalState.IsGrippingWorld)
                {
                    ray.gameObject.SetActive(false);
                    widgetClicked = null;
                }
                else
                {
                    HandleRaycast();
                }
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
            VRInput.GetInstantButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, ref triggerJustClicked, ref triggerJustReleased);

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
                if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
                {
                    widgetClicked.OverrideRayEndPoint(r, ref rayEndPoint);
                }
            }

            // Lambda to avoid copy-paste.
            System.Action handleHitNothing = () =>
            {
                if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
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

            float scale = 1f / GlobalState.WorldScale;
            if (!Physics.Raycast(r, out hitInfo, 3.0f * scale, allLayersMask, QueryTriggerInteraction.Collide))
            {
                // Nothing hit
                HandleHoverPhysicObject(null);
                handleHitNothing();
                return;
            }

            bool UIHit = (null != hitInfo.transform.gameObject.GetComponent<UIElement>()) ||
                 (null != hitInfo.transform.gameObject.GetComponent<UIHandle>()) ||
                 (null != hitInfo.transform.gameObject.GetComponent<UIVolumeTag>());
            if (!UIHit)
            {
                // hit a non-UI object.
                HandleHoverPhysicObject(null);
                handleHitNothing();
                return;
            }

            // detect if the first ray was shoot from inside an object
            Ray backRay = new Ray(hitInfo.point - 0.01f * worldDirection, -worldDirection);
            float d = hitInfo.distance - 0.01f;
            bool raycastOK = Physics.Raycast(backRay, out hitInfo, hitInfo.distance, allLayersMask, QueryTriggerInteraction.Collide);
            if (raycastOK && hitInfo.distance < d)
            {
                HandleHoverPhysicObject(null);
                handleHitNothing();
                return;
            }

            //
            // Raycast ALL UI elements
            //

            RaycastHit[] hits;
            //int layersMask = LayerMask.GetMask(new string[] { "CameraHidden", "SelectionCameraHidden", "HoverCameraHidden", "Default" });
            int layersMask = LayerMask.GetMask(new string[] { "CameraHidden", "SelectionCameraHidden", "HoverCameraHidden" });
            hits = Physics.RaycastAll(r, 3.0f * scale, layersMask, QueryTriggerInteraction.Collide);
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
                bool physicIsHit = false;

                float closestVolumeDistance = Mathf.Infinity;
                float closestWidgetDistance = Mathf.Infinity;
                float closestHandleDistance = Mathf.Infinity;
                float closestPhysicDistance = Mathf.Infinity;

                Vector3 volumeCollisionPoint = Vector3.zero;
                Vector3 widgetCollisionPoint = Vector3.zero;
                Vector3 handleCollisionPoint = Vector3.zero;
                Vector3 physicCollisionPoint = Vector3.zero;

                UIElement widget = null;
                UIHandle handle = null;
                UIVolumeTag volume = null;
                GameObject physic = null;

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

                    GameObject objectHit = hit.gameObject;
                    if (objectHit.CompareTag("PhysicObject"))
                    {
                        physicIsHit = true;
                        if (hits[i].distance < closestPhysicDistance)
                        {
                            physic = objectHit;
                            physicCollisionPoint = hits[i].point; // world space
                            closestPhysicDistance = hits[i].distance;
                        }
                    }
                }



                //
                // Send messages and states to the widget hit, with priorities.
                //


                //else if (handleIsHit)
                //{
                //    HandleHoverPhysicObject(null);

                //    if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
                //    {
                //        ActivateRay(true);
                //        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                //    }
                //    else
                //    {
                //        ActivateRay(false);
                //    }
                //    HandleRayOutOfWidget(triggerJustReleased);
                //}
                if (widgetIsHit)
                {
                    HandleHoverPhysicObject(null);

                    if (prevWidget != widget) // change widget
                    {
                        if (WidgetIsClicked) // trigger is held pushed.
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
                        if (WidgetIsClicked) // trigger is held pushed.
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
                                //widgetClicked.OnRayHover(r); // simple hover without the click effect.

                                // do nothing for the new widget.
                            }
                        }
                        else
                        {
                            if (!widget.IgnoreRayInteraction())
                                widget.OnRayHover(r);
                        }
                    }

                    // "Just click" is independant of whether we stay or change hit widget.
                    if (triggerJustClicked)
                    {
                        if (!widget.IgnoreRayInteraction())
                        {
                            widget.OnRayClick();
                            SoundManager.Instance.Play3DSound(audioSource, SoundManager.Sounds.ClickIn);
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
                        if (WidgetIsClicked)
                        {
                            if (widgetClicked == widget)
                            {
                                if (!widget.IgnoreRayInteraction())
                                {
                                    widget.OnRayReleaseInside();
                                    SoundManager.Instance.Play3DSound(audioSource, SoundManager.Sounds.ClickOut);
                                    UIElement.ClickHapticFeedback();
                                }
                            }
                            else
                            {
                                // clear state of previously clicked widget
                                if (!widgetClicked.IgnoreRayInteraction())
                                {
                                    if (widgetClicked.OnRayReleaseOutside())
                                    {
                                        SoundManager.Instance.Play3DSound(audioSource, SoundManager.Sounds.ClickOut);
                                        UIElement.ClickHapticFeedback();
                                    }
                                }

                                // give the new widget a chance to play some OnHover animation.
                                if (!widget.IgnoreRayInteraction())
                                    widget.OnRayEnter();
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Just Released received without having clicked before on any widget!!");
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

                    if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
                    {
                        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                    }
                    else
                    {
                        ray.SetParameters(worldStart, widgetCollisionPoint, newWorldDirection);
                    }
                }
                else if (physicIsHit)
                {
                    if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
                    {
                        ActivateRay(true);
                        ray.SetParameters(worldStart, rayEndPoint, newWorldDirection);
                        HandleHoverPhysicObject(null);
                    }
                    else
                    {
                        ActivateRay(true);
                        ray.SetParameters(worldStart, physicCollisionPoint, newWorldDirection);
                        HandleHoverPhysicObject(physic);
                    }
                    HandleRayOutOfWidget(triggerJustReleased);
                }
                else if (volumeIsHit)
                {
                    //HandleHoverPhysicObject(null);

                    ray.gameObject.SetActive(true);
                    if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
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
                    HandleHoverPhysicObject(null);

                    if (WidgetIsClicked && widgetClicked.OverridesRayEndPoint())
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
                HandleHoverPhysicObject(null);
                handleHitNothing();
            }
        }

        private void HandleHoverPhysicObject(GameObject gObj)
        {
            //Selection.SetHoveredObject(gObj);

            //TitleBarIsGripped

            if (gObj == null) // REMOVE
            {
                if (hoveredObject != null) // Only if we had hovered an object with the cursor => Dont un-hover general objects.
                {
                    if (TitleBarIsGripped)
                    {

                    }
                    else
                    {
                        Selection.HoveredObject = null;
                        hoveredObject = null;
                        ToolsManager.PopWindowTool();
                    }

                    // NOTE: problem here if the ray goes out of the titlebar, and we are GRIPPED on it. It will disable the tool and drop the window.
                    // TOOD: find a way here to know that we are gripped with the WindowTool.
                }
            }
            else // ADD
            {
                if (hoveredObject != gObj) // Only once for gObj
                {
                    Selection.HoveredObject = gObj; // will automatically switch with previously hovered object (UI or other).
                    hoveredObject = gObj;

                    if (null != gObj.GetComponent<UIHandle>())
                    {
                        ToolsManager.PushWindowTool();
                    }
                }
            }
        }

        private void HandleRayOutOfWidget(bool triggerJustReleased)
        {
            if (WidgetIsClicked) // trigger is held pushed.
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
                        if (widgetClicked.OnRayReleaseOutside())
                        {
                            SoundManager.Instance.Play3DSound(audioSource, SoundManager.Sounds.ClickOut);
                            UIElement.ClickHapticFeedback();
                        }
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