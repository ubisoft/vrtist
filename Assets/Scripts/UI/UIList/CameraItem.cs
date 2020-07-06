﻿using UnityEngine;

namespace VRtist
{
    public class CameraItem : ListItemContent
    {
        public GameObject cameraObject;
        public UIDynamicListItem item;

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
            Selection.OnActiveCameraChanged += OnActiveCameraChanged;
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
            Selection.OnActiveCameraChanged -= OnActiveCameraChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            if(Selection.IsSelected(cameraObject))
            {
                SetColor(UIOptions.PushedColor);
            }
            else
            {
                SetColor(UIOptions.BackgroundColor);
            }
        }

        private void OnActiveCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            if(args.activeCamera == cameraObject)
            {
                SetColor(UIOptions.SceneHoverColor);
            }
            else
            {
                if(Selection.IsSelected(cameraObject))
                {
                    SetColor(UIOptions.PushedColor);
                }
                else
                {
                    SetColor(UIOptions.BackgroundColor);
                }
            }
        }

        public void SetColor(Color color)
        {
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[0].SetColor("_BaseColor", color);
        }

        public void SetCameraObject(GameObject cameraObject)
        {
            this.cameraObject = cameraObject;
            Camera cam = cameraObject.GetComponentInChildren<Camera>(true);
            SetColor(UIOptions.BackgroundColor);
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[1].SetTexture("_UnlitColorMap", cam.targetTexture);
        }
    }
}
