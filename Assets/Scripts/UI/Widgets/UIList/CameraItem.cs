using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace VRtist
{
    public class CameraItem : ListItemContent
    {
        [HideInInspector] public GameObject cameraObject;
        [HideInInspector] public UIDynamicListItem item;

        public void Start()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            CameraManager.Instance.onActiveCameraChanged.AddListener(OnActiveCameraChanged);
        }

        public void OnDestroy()
        {
            Selection.onSelectionChanged.RemoveListener(OnSelectionChanged);
            CameraManager.Instance.onActiveCameraChanged.RemoveListener(OnActiveCameraChanged);
        }

        private void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> currentSelectedObjects)
        {
            if (Selection.IsSelected(cameraObject))
            {
                SetColor(UIOptions.PushedColor);
            }
            else
            {
                SetColor(UIOptions.BackgroundColor);
            }
        }

        private void OnActiveCameraChanged(GameObject _, GameObject activeCamera)
        {
            if (activeCamera == cameraObject)
            {
                SetColor(UIOptions.SceneHoverColor);
            }
            else
            {
                if (Selection.IsSelected(cameraObject))
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

        public void SetItemName(string name)
        {
            TextMeshProUGUI text = transform.Find("Canvas/Panel/Text").gameObject.GetComponent<TextMeshProUGUI>();
            text.text = name;
        }

        public void SetCameraObject(GameObject cameraObject)
        {
            this.cameraObject = cameraObject;
            Camera cam = cameraObject.GetComponentInChildren<Camera>(true);
            SetColor(UIOptions.BackgroundColor);
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[1].SetTexture("_UnlitColorMap", cam.targetTexture);
            SetItemName(cameraObject.name);
        }
    }
}
