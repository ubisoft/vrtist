/* MIT License
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
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    [Serializable]
    public class ActiveCameraChangedEvent : UnityEvent<GameObject, GameObject>
    {
        // Empty
    }

    /// <summary>
    /// Manage the current active camera.
    /// </summary>
    public class CameraManager
    {
        private GameObject virtualCamera;
        private Camera virtualCameraComponent;
        private GameObject activeCamera = null;
        RenderTexture renderTexture;
        public RenderTexture RenderTexture
        {
            get
            {
                if (null == renderTexture)
                {
                    renderTexture = new RenderTexture(currentResolution.width, currentResolution.height, 0, RenderTextureFormat.ARGB32);
                    if (null == renderTexture)
                        Debug.LogError("CAMERA FAILED");
                    renderTexture.name = "Camera RT";

                    _ = VirtualCamera;  // be sure virtualCameraComponent exists
                    virtualCameraComponent.targetTexture = renderTexture;
                }
                return renderTexture;
            }
        }

        Texture2D emptyTexture;
        public Texture2D EmptyTexture
        {
            get
            {
                if (null == emptyTexture)
                {
                    emptyTexture = new Texture2D(currentResolution.width, currentResolution.height, TextureFormat.RGB24, false);
                    Utils.FillTexture(emptyTexture, new Color(10f / 255f, 10f / 255f, 10f / 255f));  // almost black: black is ignored :(
                }
                return emptyTexture;
            }
        }

        [System.Serializable]
        public struct Resolution : IEquatable<Resolution>
        {
            public int width;
            public int height;

            public override bool Equals(object obj)
            {
                if (obj is Resolution)
                    return this.Equals((Resolution)obj);
                return false;
            }

            public override int GetHashCode()
            {
                return width ^ height;
            }

            public bool Equals(Resolution r)
            {
                return r.width == width && r.height == height;
            }

            public static bool operator !=(Resolution r1, Resolution r2)
            {
                return !r1.Equals(r2);
            }

            public static bool operator ==(Resolution r1, Resolution r2)
            {
                return r1.Equals(r2);
            }
        }

        public static Resolution resolution720p = new Resolution { width = 1280, height = 720 };
        public static Resolution resolution1080p = new Resolution { width = 1920, height = 1080 };
        public static Resolution resolution2160p = new Resolution { width = 3840, height = 2160 };
        private Resolution realTimeResolution;
        public Resolution videoOutputResolution;

        private Resolution currentResolution;
        public Resolution CurrentResolution
        {
            get
            {
                return currentResolution;
            }
            set
            {
                if (value.width == currentResolution.width && value.height == currentResolution.height)
                    return;
                currentResolution = value;
                if (null != renderTexture)
                {
                    RenderTexture.active = null;
                    renderTexture.Release();
                    renderTexture = null;
                }
                if (null != emptyTexture)
                {
                    Texture2D.Destroy(emptyTexture);
                    emptyTexture = null;
                }

                AssignTextures();
            }
        }

        public enum VideoResolution
        {
            VideoResolution_Unknown,
            VideoResolution_720p,
            VideoResolution_1080p,
            VideoResolution_2160p,
        }

        public VideoResolution OutputResolution
        {
            get
            {
                switch (videoOutputResolution.height)
                {
                    case 720: return VideoResolution.VideoResolution_720p;
                    case 1080: return VideoResolution.VideoResolution_1080p;
                    case 2160: return VideoResolution.VideoResolution_2160p;
                    default: return VideoResolution.VideoResolution_Unknown;
                }
            }
            set
            {
                switch (value)
                {
                    case VideoResolution.VideoResolution_720p:
                        videoOutputResolution = resolution720p;
                        break;
                    case VideoResolution.VideoResolution_1080p:
                        videoOutputResolution = resolution1080p;
                        break;
                    case VideoResolution.VideoResolution_2160p:
                        videoOutputResolution = resolution2160p;
                        break;
                    default:
                        videoOutputResolution = resolution1080p;
                        break;
                }
                GlobalState.Settings.videoOutputResolution = videoOutputResolution;
            }
        }

        List<Material> screens = new List<Material>();
        bool isVideoOutput = false;

        GameObject VirtualCamera
        {
            get
            {
                if (null == virtualCamera)
                {
                    virtualCamera = new GameObject("Virtual Camera");

                    virtualCameraComponent = virtualCamera.AddComponent<Camera>();
                    _ = RenderTexture;
                    virtualCameraComponent.cullingMask = LayerMask.GetMask(new string[] { "Default", "TransparentFX", "Water", "Selection", "Hover" });
                    virtualCameraComponent.nearClipPlane = 0.07f;
                    virtualCameraComponent.farClipPlane = 1000f;

                    HDAdditionalCameraData additionCameraData = virtualCamera.AddComponent<HDAdditionalCameraData>();
                    additionCameraData.volumeLayerMask = LayerMask.GetMask(new string[] { "PostProcessing", "CameraPostProcessing" });
                    additionCameraData.stopNaNs = true;
                }
                return virtualCamera;
            }
        }
        public GameObject ActiveCamera
        {
            get { return activeCamera; }
            set
            {
                if (activeCamera == value)
                    return;

                GameObject previousActiveCamera = activeCamera;
                activeCamera = value;
                if (null != previousActiveCamera)
                {
                    CameraController cameraController = previousActiveCamera.GetComponent<CameraController>();
                    cameraController.SetVirtualCamera(null);
                }
                if (null != activeCamera)
                {
                    // reparent virtual camera to active camera
                    VirtualCamera.transform.parent = activeCamera.transform.Find("Rotate");
                    VirtualCamera.transform.localPosition = Vector3.zero;
                    VirtualCamera.transform.localRotation = Quaternion.identity;
                    VirtualCamera.transform.localScale = Vector3.one;
                    VirtualCamera.SetActive(true);

                    // apply active parameters to virtual camera
                    CameraController cameraController = activeCamera.GetComponent<CameraController>();
                    cameraController.SetVirtualCamera(virtualCameraComponent);

                    if (isVideoOutput)
                        activeCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", EmptyTexture);
                    else
                        activeCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", virtualCameraComponent.targetTexture);
                }
                else
                {
                    VirtualCamera.SetActive(false);
                }
                onActiveCameraChanged.Invoke(previousActiveCamera, activeCamera);
                AssignTextures();
            }
        }
        public ActiveCameraChangedEvent onActiveCameraChanged = new ActiveCameraChangedEvent();

        static CameraManager instance = null;
        public static CameraManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new CameraManager();
                }
                return instance;
            }
        }

        CameraManager()
        {
            realTimeResolution = resolution720p;
            videoOutputResolution = GlobalState.Settings.videoOutputResolution;
            CurrentResolution = realTimeResolution;

            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            Selection.onHoveredChanged.AddListener(OnHoveredChanged);
            Selection.onAuxiliarySelectionChanged.AddListener(OnAuxiliaryChanged);
            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);
        }

        public void RegisterScreen(Material material)
        {
            screens.Add(material);
            AssignTextures();
        }
        public void UnregisterScreen(Material material)
        {
            screens.Remove(material);
        }

        void AssignTextures()
        {
            foreach (Material material in screens)
            {
                material.SetTexture("_UnlitColorMap", (isVideoOutput || null == ActiveCamera) ? EmptyTexture as Texture : RenderTexture as Texture);
            }
            if (null != ActiveCamera)
            {
                ActiveCamera.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", isVideoOutput ? EmptyTexture as Texture : RenderTexture as Texture);
            }
        }

        void OnAnimationStateChanged(AnimationState state)
        {
            if (AnimationState.VideoOutput == GlobalState.Animation.animationState)
            {
                isVideoOutput = true;
                CurrentResolution = videoOutputResolution;
            }
            else if (isVideoOutput)
            {
                isVideoOutput = false;
                CurrentResolution = realTimeResolution;
            }
        }

        public Camera GetActiveCameraComponent()
        {
            if (null == ActiveCamera) { return null; }
            return virtualCameraComponent;
        }

        GameObject GetFirstCamera(HashSet<GameObject> objects)
        {
            foreach (GameObject o in objects)
            {
                if (null != o && null != o.GetComponent<CameraController>())
                    return o;
            }
            return null;
        }

        void UpdateActiveCamera(GameObject hoveredObject, HashSet<GameObject> currentSelection)
        {
            // --------------------------------------------
            // Check hover
            // --------------------------------------------
            GameObject hoveredCamera = null;
            if (null != hoveredObject && null != hoveredObject.GetComponent<CameraController>())
                hoveredCamera = hoveredObject;

            // Set current active camera from hovered one
            if (null != hoveredCamera && (hoveredCamera != ActiveCamera || Selection.IsSelected(hoveredCamera)))
            {
                // Enable current active camera
                ActiveCamera = hoveredCamera;
                return;
            }

            // --------------------------------------------
            // Check selected
            // --------------------------------------------
            GameObject selectedCamera = GetFirstCamera(currentSelection);
            if (null != selectedCamera && selectedCamera != ActiveCamera)
            {
                // Enable new one
                ActiveCamera = selectedCamera;
                return;
            }

            //if (!GlobalState.Animation.IsAnimating() && null == hoveredCamera && null == selectedCamera)
            //{
            //    ActiveCamera = null;
            //}
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            UpdateActiveCamera(Selection.HoveredObject, currentSelection);
        }

        void OnHoveredChanged(GameObject previousHover, GameObject currentHover)
        {
            UpdateActiveCamera(currentHover, Selection.SelectedObjects);
        }

        void OnAuxiliaryChanged(GameObject previousAuxiliary, GameObject currentAuxiliary)
        {
            UpdateActiveCamera(currentAuxiliary, Selection.SelectedObjects);
        }

        public void Clear()
        {
            // Be sure to never delete the virtual cam when loading a new scene
            VirtualCamera.transform.parent = Utils.FindRootGameObject("UIUtils").transform;
        }
    }
}
